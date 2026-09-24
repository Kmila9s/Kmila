using System.Text;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// A <see cref="TextWriter"/> that forwards every completed line written to it
/// into an <see cref="ILogger"/> at a configurable level. Wired up by the Web
/// and MAUI hosts via <c>Console.SetOut</c> so that legacy <c>Console.WriteLine</c>
/// calls in our referenced Parser / Interpreter / TimeMachine projects flow
/// into the in-app Logs page instead of only appearing on a terminal that
/// mobile / MAUI users can't see.
/// </summary>
/// <remarks>
/// Buffers partial writes until a newline so that multi-part Write calls end
/// up as a single log entry (matches how ILogger treats one message per call).
/// Thread-safe via a lock around the buffer — Console.Out is shared across the
/// process.
/// </remarks>
public sealed class ConsoleToLoggerWriter : TextWriter
{
    private readonly ILogger _logger;
    private readonly LogLevel _level;
    private readonly StringBuilder _buffer = new();
    private readonly object _gate = new();

    public ConsoleToLoggerWriter(ILogger logger, LogLevel level = LogLevel.Debug)
    {
        _logger = logger;
        _level = level;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        lock (_gate)
        {
            if (value == '\n') Flush();
            else if (value != '\r') _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        lock (_gate)
        {
            foreach (var ch in value)
            {
                if (ch == '\n') Flush();
                else if (ch != '\r') _buffer.Append(ch);
            }
        }
    }

    public override void WriteLine(string? value)
    {
        Write(value);
        lock (_gate) Flush();
    }

    public override void WriteLine()
    {
        lock (_gate) Flush();
    }

    public override void Flush()
    {
        // Caller already holds _gate when this runs from the writer, but
        // guard for external flushers (e.g. test harnesses) too.
        if (!System.Threading.Monitor.IsEntered(_gate))
        {
            lock (_gate) FlushLocked();
            return;
        }
        FlushLocked();
    }

    private void FlushLocked()
    {
        if (_buffer.Length == 0) return;
        var line = _buffer.ToString();
        _buffer.Clear();
        _logger.Log(_level, "{ConsoleLine}", line);
    }
}
