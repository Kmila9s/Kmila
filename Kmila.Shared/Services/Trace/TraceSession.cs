using System.IO.Compression;
using System.Threading.Channels;
using Interpreter.Services;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;
using Parser.Models;
using SQLite;

namespace Kmila.Shared.Services.Trace;

/// <summary>
/// Long-run Phase 2: the on-disk trace of ONE simulation run.
/// <para>
/// <b>Write side.</b> <see cref="Attach"/> hooks the engine's
/// <see cref="SignalHistory.OnEvicting"/>: every batch the in-memory cap is about to
/// drop is queued (non-blocking) to a background writer, which collapses it with
/// <see cref="WaveformTransitionCollapser"/> (identical rules to
/// <see cref="WaveformData.FromSignalHistory"/>) and appends compressed chunks of
/// transitions to a per-run SQLite file (WAL, batched transactions). At the end of the
/// run <see cref="CompleteAsync"/> also spills the samples still held in memory, so the
/// file then holds the run's COMPLETE history.
/// </para>
/// <para>
/// <b>Read side.</b> <see cref="AttachArchives"/> gives each <see cref="WaveformSignal"/>
/// of the post-run waveform an <see cref="ITransitionArchive"/>; the viewer, VCD export
/// and cursor lookups then page history in from disk instead of holding it in RAM.
/// </para>
/// A run that never hits the cap never creates a file and attaches nothing — short
/// runs behave exactly as before.
/// </summary>
public sealed class TraceSession : IDisposable
{
    private const int ChunkSize = 1024; // transitions per stored chunk

    /// <summary>
    /// Backpressure bound: evicted samples queued for the writer but not yet stored.
    /// Above it the simulation thread waits for the writer (down to half) instead of
    /// piling boxed samples up in RAM — on slow phone flash a fast run would
    /// otherwise just move the memory growth from the history into this queue.
    /// </summary>
    internal const long MaxPendingSamples = 1_000_000;

    private readonly string _path;
    private readonly long _tickFs;
    private readonly TraceStorage _storage;
    private readonly ILogger _log;
    private readonly Channel<Batch> _queue =
        Channel.CreateUnbounded<Batch>(new UnboundedChannelOptions { SingleReader = true });
    private readonly object _dbLock = new();

    private Task? _writer;
    private SQLiteConnection? _db;
    private long _bytesWritten;
    private long _pendingSamples;
    private volatile bool _spilled;
    private volatile bool _truncated;
    private volatile bool _disposed;
    private SignalHistory? _history;

    // Writer-thread state.
    private readonly Dictionary<Variable, SignalState> _signals = new();

    private readonly record struct Batch(Variable? Signal, (long TimeMoment, object Value, int SourceLine)[]? Samples);

    private sealed class SignalState
    {
        public int Id;
        public required string Name;
        public required string DataType;
        public required int BitWidth;
        public required string PortType;
        public required WaveformTransitionCollapser Collapser;
        public readonly List<WaveformTransition> Buffer = new(ChunkSize);
        public long Count;
    }

    internal TraceSession(string path, long tickFs, TraceStorage storage, ILogger log)
    {
        _path = path;
        _tickFs = tickFs;
        _storage = storage;
        _log = log;
    }

    /// <summary>Path of the trace file (exists only once something was spilled).</summary>
    public string FilePath => _path;

    /// <summary>True once the cap evicted at least one batch (a file exists).</summary>
    public bool HasSpilled => _spilled;

    /// <summary>True when the disk guard stopped spilling — the archive is incomplete.</summary>
    public bool IsTruncated => _truncated;

    /// <summary>True when <see cref="CompleteAsync"/> finished and the file holds the whole run.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>Compressed bytes stored so far (read by the run watchdog).</summary>
    public long BytesWritten => Interlocked.Read(ref _bytesWritten);

    /// <summary>Evicted samples queued for the writer but not yet stored.</summary>
    public long PendingSamples => Interlocked.Read(ref _pendingSamples);

    /// <summary>
    /// True when this run's trace is close enough to the storage limits that the run
    /// should stop now, while the archive can still be completed (see
    /// <see cref="TraceStorage.IsNearLimit"/>). Also true once the hard guard tripped.
    /// </summary>
    public bool IsNearLimit => _truncated || (_spilled && _storage.IsNearLimit(BytesWritten));

    /// <summary>Hooks the engine's history so evicted batches are spilled here.</summary>
    public void Attach(SignalHistory history)
    {
        _history = history;
        history.OnEvicting = OnEvicting;
    }

    // Called on the sim thread under the history lock: enqueue only.
    private void OnEvicting(Variable signal, IReadOnlyList<(long TimeMoment, object Value, int SourceLine)> samples)
    {
        if (_truncated || _disposed) return;
        if (!_spilled)
        {
            _spilled = true;
            _writer = Task.Run(WriterLoopAsync);
        }
        var copy = samples as (long, object, int)[] ?? samples.ToArray();
        long pending = Interlocked.Add(ref _pendingSamples, copy.Length);
        _queue.Writer.TryWrite(new Batch(signal, copy));

        // Backpressure: let the writer catch up rather than buffering without bound.
        if (pending > MaxPendingSamples)
        {
            while (Interlocked.Read(ref _pendingSamples) > MaxPendingSamples / 2
                   && !_truncated && !_disposed && _writer is { IsCompleted: false })
            {
                Thread.Sleep(5);
            }
        }
    }

    /// <summary>
    /// End of run: spills the samples still held in memory, drains the writer and
    /// seals the file. No-op (returns false) when nothing was ever spilled — the
    /// run fit in memory and needs no archive. Call after the engine stopped recording.
    /// </summary>
    public async Task<bool> CompleteAsync()
    {
        var history = _history;
        if (history != null) history.OnEvicting = null;
        if (!_spilled || _disposed) return false;

        if (history != null && !_truncated)
        {
            foreach (var sig in history.GetTrackedSignals())
            {
                var tail = history.GetSignalHistoryDetailed(sig);
                if (tail.Count == 0) continue;
                var arr = new (long, object, int)[tail.Count];
                for (int i = 0; i < tail.Count; i++)
                    arr[i] = (tail[i].TimeMoment, tail[i].Value, tail[i].SourceLine);
                Interlocked.Add(ref _pendingSamples, arr.Length);
                _queue.Writer.TryWrite(new Batch(sig, arr));
            }
        }
        _queue.Writer.TryComplete();
        if (_writer != null)
        {
            try { await _writer.ConfigureAwait(false); }
            catch (Exception ex) { _log.LogWarning(ex, "[Trace] writer failed"); _truncated = true; }
        }
        IsComplete = !_truncated && !_disposed;
        return IsComplete;
    }

    // ── Writer ───────────────────────────────────────────────────────────────

    private async Task WriterLoopAsync()
    {
        try
        {
            lock (_dbLock)
            {
                _db = new SQLiteConnection(_path,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                _db.ExecuteScalar<string>("PRAGMA journal_mode=WAL");
                _db.Execute("PRAGMA synchronous=NORMAL");
                _db.Execute("CREATE TABLE IF NOT EXISTS Signal (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, " +
                            "DataType TEXT NOT NULL, BitWidth INTEGER NOT NULL, PortType TEXT NOT NULL, Count INTEGER NOT NULL)");
                _db.Execute("CREATE TABLE IF NOT EXISTS Chunk (SignalId INTEGER NOT NULL, StartNs INTEGER NOT NULL, " +
                            "EndNs INTEGER NOT NULL, Count INTEGER NOT NULL, Data BLOB NOT NULL)");
                _db.Execute("CREATE INDEX IF NOT EXISTS IX_Chunk ON Chunk (SignalId, StartNs)");
            }

            var reader = _queue.Reader;
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                lock (_dbLock)
                {
                    if (_disposed) return;
                    _db!.BeginTransaction();
                    try
                    {
                        // Drain whatever is queued into one transaction.
                        int n = 0;
                        while (n < 4096 && reader.TryRead(out var batch))
                        {
                            n++;
                            if (batch.Samples != null) Interlocked.Add(ref _pendingSamples, -batch.Samples.Length);
                            if (batch.Signal == null || batch.Samples == null || _truncated) continue;
                            var st = GetState(batch.Signal);
                            foreach (var (tm, value, line) in batch.Samples)
                            {
                                if (st.Collapser.Add(tm, value, line, out var fin))
                                {
                                    st.Buffer.Add(fin);
                                    if (st.Buffer.Count >= ChunkSize) WriteChunk(st);
                                }
                            }
                        }
                        _db.Commit();
                    }
                    catch
                    {
                        try { _db.Rollback(); } catch { /* ignore */ }
                        throw;
                    }
                }
            }

            // Queue completed: release pending transitions, flush buffers, record counts.
            lock (_dbLock)
            {
                if (_disposed) return;
                _db!.BeginTransaction();
                foreach (var st in _signals.Values)
                {
                    if (st.Collapser.Flush(out var last)) st.Buffer.Add(last);
                    if (st.Buffer.Count > 0) WriteChunk(st);
                    _db.Execute("UPDATE Signal SET Count = ? WHERE Id = ?", st.Count, st.Id);
                }
                _db.Commit();
                // Returns a row (busy, log, checkpointed) — ExecuteScalar, not Execute
                // (sqlite-net's Execute treats SQLITE_ROW as an error).
                _db.ExecuteScalar<int>("PRAGMA wal_checkpoint(TRUNCATE)");
            }
        }
        catch (Exception ex)
        {
            _truncated = true;
            _log.LogWarning(ex, "[Trace] writer stopped; archive disabled for this run ({Path})", _path);
        }
    }

    private SignalState GetState(Variable signal)
    {
        if (_signals.TryGetValue(signal, out var st)) return st;
        var (dataType, bitWidth, portType) = WaveformData.DescribeSignal(signal);
        st = new SignalState
        {
            Name = signal.Name,
            DataType = dataType,
            BitWidth = bitWidth,
            PortType = portType,
            Collapser = new WaveformTransitionCollapser(dataType, bitWidth, _tickFs),
        };
        _db!.Execute("INSERT INTO Signal (Name, DataType, BitWidth, PortType, Count) VALUES (?, ?, ?, ?, 0)",
            st.Name, st.DataType, st.BitWidth, st.PortType);
        st.Id = (int)_db.ExecuteScalar<long>("SELECT last_insert_rowid()");
        _signals[signal] = st;
        return st;
    }

    private void WriteChunk(SignalState st)
    {
        var buf = st.Buffer;
        if (buf.Count == 0) return;
        if (_bytesWritten > _storage.MaxTraceBytes || !_storage.HasFreeSpace(_storage.MinFreeBytes))
        {
            _truncated = true;
            buf.Clear();
            _log.LogWarning("[Trace] disk guard hit ({Bytes} bytes written) — run archive disabled", _bytesWritten);
            return;
        }
        var data = TraceChunkCodec.Encode(buf);
        _db!.Execute("INSERT INTO Chunk (SignalId, StartNs, EndNs, Count, Data) VALUES (?, ?, ?, ?, ?)",
            st.Id, buf[0].TimeNs, buf[^1].TimeNs, buf.Count, data);
        Interlocked.Add(ref _bytesWritten, data.Length);
        st.Count += buf.Count;
        buf.Clear();
    }

    // ── Read side ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gives every signal of <paramref name="waveform"/> that the trace holds an
    /// on-disk archive. Only after a successful <see cref="CompleteAsync"/> — an
    /// incomplete (truncated) trace is never used, so the viewer falls back to the
    /// in-memory window rather than showing a history with holes. Returns how many
    /// signals got an archive.
    /// </summary>
    public int AttachArchives(WaveformData waveform)
    {
        if (!IsComplete || _disposed || waveform == null) return 0;
        Dictionary<string, (int Id, long Count)> byName;
        lock (_dbLock)
        {
            if (_db == null) return 0;
            byName = _db.Query<SignalRow>("SELECT Id, Name, Count FROM Signal")
                        .ToDictionary(r => r.Name, r => (r.Id, r.Count), StringComparer.Ordinal);
        }
        int attached = 0;
        foreach (var sig in waveform.Signals)
        {
            if (byName.TryGetValue(sig.Name, out var info) && info.Count > 0)
            {
                sig.Archive = new Archive(this, info.Id, info.Count);
                attached++;
            }
        }
        return attached;
    }

    private sealed class SignalRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Count { get; set; }
    }

    private sealed class ChunkRow
    {
        public long StartNs { get; set; }
        public long EndNs { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    private ChunkRow? QueryLastChunkAtOrBefore(int signalId, long ns)
    {
        lock (_dbLock)
        {
            if (_disposed || _db == null) return null;
            return _db.Query<ChunkRow>(
                "SELECT StartNs, EndNs, Data FROM Chunk WHERE SignalId = ? AND StartNs <= ? " +
                "ORDER BY StartNs DESC LIMIT 1", signalId, ns).FirstOrDefault();
        }
    }

    private List<ChunkRow> QueryChunkPage(int signalId, long fromNs, long toNs, long afterStart, int pageSize)
    {
        lock (_dbLock)
        {
            if (_disposed || _db == null) return new List<ChunkRow>();
            return _db.Query<ChunkRow>(
                "SELECT StartNs, EndNs, Data FROM Chunk WHERE SignalId = ? AND EndNs >= ? AND StartNs <= ? " +
                "AND StartNs > ? ORDER BY StartNs LIMIT ?", signalId, fromNs, toNs, afterStart, pageSize);
        }
    }

    /// <summary>Per-signal reader over the trace file.</summary>
    private sealed class Archive : ITransitionArchive
    {
        private readonly TraceSession _owner;
        private readonly int _signalId;
        private long _cachedStart = long.MinValue;
        private WaveformTransition[]? _cached;

        public Archive(TraceSession owner, int signalId, long count)
        {
            _owner = owner; _signalId = signalId; Count = count;
        }

        public long Count { get; }

        public WaveformTransition? LastAtOrBefore(long ns)
        {
            var row = _owner.QueryLastChunkAtOrBefore(_signalId, ns);
            if (row == null) return null;
            var items = Decode(row);
            int lo = 0, hi = items.Length - 1, res = -1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (items[mid].TimeNs <= ns) { res = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return res < 0 ? null : items[res];
        }

        public IEnumerable<WaveformTransition> ReadRange(long fromNs, long toNs)
        {
            long after = long.MinValue;
            while (true)
            {
                var page = _owner.QueryChunkPage(_signalId, fromNs, toNs, after, 32);
                if (page.Count == 0) yield break;
                foreach (var row in page)
                {
                    after = row.StartNs;
                    foreach (var t in Decode(row))
                    {
                        if (t.TimeNs < fromNs) continue;
                        if (t.TimeNs > toNs) yield break;
                        yield return t;
                    }
                }
            }
        }

        // One-chunk cache: cursor scrubbing hits the same chunk repeatedly.
        private WaveformTransition[] Decode(ChunkRow row)
        {
            if (_cached != null && _cachedStart == row.StartNs) return _cached;
            var items = TraceChunkCodec.Decode(row.Data, row.StartNs);
            _cached = items; _cachedStart = row.StartNs;
            return items;
        }
    }

    /// <summary>Closes the file and deletes it (and its WAL/SHM sidecars).</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_history != null) _history.OnEvicting = null;
        _queue.Writer.TryComplete();
        lock (_dbLock)
        {
            try { _db?.Close(); } catch { /* ignore */ }
            _db = null;
        }
        foreach (var f in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            try { if (File.Exists(f)) File.Delete(f); } catch { /* best effort; swept on next start */ }
        }
    }
}

/// <summary>
/// Compact binary form of a run of <see cref="WaveformTransition"/>s (one stored
/// chunk): count, then per transition the time delta (7-bit varint from the previous
/// one; the first is relative to the chunk start), a flags byte, and the optional
/// value / label / bits / source line — Deflate-compressed.
/// </summary>
internal static class TraceChunkCodec
{
    private const byte HasValue = 1, HasLabel = 2, HasBits = 4, BitsEqLabel = 8;

    public static byte[] Encode(IReadOnlyList<WaveformTransition> items)
    {
        using var ms = new MemoryStream();
        using (var z = new DeflateStream(ms, CompressionLevel.Fastest, leaveOpen: true))
        using (var w = new BinaryWriter(z))
        {
            w.Write7BitEncodedInt(items.Count);
            long prev = items.Count > 0 ? items[0].TimeNs : 0;
            foreach (var t in items)
            {
                w.Write7BitEncodedInt64(t.TimeNs - prev);
                prev = t.TimeNs;
                byte flags = 0;
                if (t.Value.HasValue) flags |= HasValue;
                if (t.Label != null) flags |= HasLabel;
                if (t.Bits != null)
                {
                    flags |= HasBits;
                    if (t.Label != null && string.Equals(t.Bits, t.Label, StringComparison.Ordinal)) flags |= BitsEqLabel;
                }
                w.Write(flags);
                if (t.Value.HasValue) w.Write(t.Value.Value);
                if (t.Label != null) w.Write(t.Label);
                if (t.Bits != null && (flags & BitsEqLabel) == 0) w.Write(t.Bits);
                w.Write7BitEncodedInt(t.SourceLine);
            }
        }
        return ms.ToArray();
    }

    public static WaveformTransition[] Decode(byte[] data, long startNs)
    {
        using var ms = new MemoryStream(data);
        using var z = new DeflateStream(ms, CompressionMode.Decompress);
        using var r = new BinaryReader(z);
        int n = r.Read7BitEncodedInt();
        var items = new WaveformTransition[n];
        long t = startNs;
        for (int i = 0; i < n; i++)
        {
            t += r.Read7BitEncodedInt64();
            byte flags = r.ReadByte();
            double? value = (flags & HasValue) != 0 ? r.ReadDouble() : null;
            string? label = (flags & HasLabel) != 0 ? r.ReadString() : null;
            string? bits = (flags & HasBits) == 0 ? null
                         : (flags & BitsEqLabel) != 0 ? label
                         : r.ReadString();
            int line = r.Read7BitEncodedInt();
            items[i] = new WaveformTransition(t, value, label, line, bits);
        }
        return items;
    }
}
