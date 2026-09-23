using System.Text;
using System.Text.Json;
using Kmila.Shared.Models.VersionControl;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.VersionControl;

/// <summary>
/// Thin GitHub Git Data API layer for the version-control engine. Every call
/// goes through <see cref="GitHubAuth.SendApiAsync"/> so the access token stays
/// encapsulated in <see cref="GitHubAuth"/>. This class only knows Git-Data
/// semantics (blobs → trees → commits → refs) and shapes; it holds no auth
/// state of its own. Returns simple values / records; a null return signals a
/// failed or empty call (details are logged + surfaced by the caller).
/// </summary>
public class GitHubRemote
{
    private readonly GitHubAuth _auth;
    private readonly ILogger<GitHubRemote> _logger;

    private const string ApiBase = "https://api.github.com";

    public GitHubRemote(GitHubAuth auth, ILogger<GitHubRemote>? logger = null)
    {
        _auth = auth;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubRemote>.Instance;
    }

    /// <summary>The signed-in account (for commit author + default owner). Null when logged out.</summary>
    public GitHubAuth.GitHubAccount? Account => _auth.Account;

    /// <summary>Whether the last operation hit a rate limit (checked by callers for messaging).</summary>
    public bool LastRateLimited { get; private set; }

    // ─── Refs ────────────────────────────────────────────────────

    /// <summary>Current commit SHA at the tip of <paramref name="branch"/>, or null
    /// when the ref/repo doesn't exist yet (empty repo).</summary>
    public async Task<string?> GetRefShaAsync(string owner, string repo, string branch, CancellationToken ct = default)
    {
        using var r = await _auth.SendApiAsync(HttpMethod.Get,
            $"{ApiBase}/repos/{owner}/{repo}/git/ref/heads/{Uri.EscapeDataString(branch)}", null, ct);
        Track(r);
        if (!r.Ok || r.Doc == null) return null;
        return r.Doc.RootElement.TryGetProperty("object", out var o)
            && o.TryGetProperty("sha", out var sha) ? sha.GetString() : null;
    }

    /// <summary>Creates or fast-forwards <paramref name="branch"/> to <paramref name="sha"/>.
    /// Creates the ref if it doesn't exist. Returns true on success.</summary>
    public async Task<bool> UpdateRefAsync(string owner, string repo, string branch, string sha, bool force, CancellationToken ct = default)
    {
        // Try PATCH first (existing ref); if it 404s, POST to create it.
        using (var patch = await _auth.SendApiAsync(HttpMethod.Patch,
            $"{ApiBase}/repos/{owner}/{repo}/git/refs/heads/{Uri.EscapeDataString(branch)}",
            new Dictionary<string, object?> { ["sha"] = sha, ["force"] = force }, ct))
        {
            Track(patch);
            if (patch.Ok) return true;
            if (patch.Status != System.Net.HttpStatusCode.NotFound
                && patch.Status != System.Net.HttpStatusCode.UnprocessableEntity)
                return false;
        }

        using var create = await _auth.SendApiAsync(HttpMethod.Post,
            $"{ApiBase}/repos/{owner}/{repo}/git/refs",
            new Dictionary<string, object?> { ["ref"] = $"refs/heads/{branch}", ["sha"] = sha }, ct);
        Track(create);
        return create.Ok;
    }

    // ─── Blobs / trees / commits ─────────────────────────────────

    /// <summary>Uploads a text blob and returns its git SHA.</summary>
    public async Task<string?> CreateBlobAsync(string owner, string repo, string content, CancellationToken ct = default)
    {
        // base64 encoding keeps arbitrary bytes intact (VHDL is text, but this
        // is robust for any content and avoids UTF-8 edge cases on the wire).
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        using var r = await _auth.SendApiAsync(HttpMethod.Post,
            $"{ApiBase}/repos/{owner}/{repo}/git/blobs",
            new Dictionary<string, object?> { ["content"] = b64, ["encoding"] = "base64" }, ct);
        Track(r);
        return r.Ok && r.Doc != null && r.Doc.RootElement.TryGetProperty("sha", out var s) ? s.GetString() : null;
    }

    /// <summary>Reads a blob's content (decoded from base64).</summary>
    public async Task<string?> GetBlobAsync(string owner, string repo, string sha, CancellationToken ct = default)
    {
        using var r = await _auth.SendApiAsync(HttpMethod.Get,
            $"{ApiBase}/repos/{owner}/{repo}/git/blobs/{sha}", null, ct);
        Track(r);
        if (!r.Ok || r.Doc == null) return null;
        var root = r.Doc.RootElement;
        if (!root.TryGetProperty("content", out var c)) return null;
        var enc = root.TryGetProperty("encoding", out var e) ? e.GetString() : "base64";
        var raw = c.GetString() ?? string.Empty;
        if (enc == "base64")
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(raw.Replace("\n", ""))); }
            catch { return raw; }
        }
        return raw;
    }

    /// <summary>
    /// Creates a tree. <paramref name="entries"/> map path → blob sha for kept/changed
    /// files; a null sha under <paramref name="baseTreeSha"/> deletes that path.
    /// </summary>
    public async Task<string?> CreateTreeAsync(string owner, string repo, string? baseTreeSha,
        IEnumerable<(string Path, string? Sha)> entries, CancellationToken ct = default)
    {
        var tree = entries.Select(e => new Dictionary<string, object?>
        {
            ["path"] = e.Path,
            ["mode"] = "100644",
            ["type"] = "blob",
            ["sha"]  = e.Sha   // null → delete under base_tree
        }).ToList();

        var payload = new Dictionary<string, object?> { ["tree"] = tree };
        if (!string.IsNullOrEmpty(baseTreeSha)) payload["base_tree"] = baseTreeSha;

        using var r = await _auth.SendApiAsync(HttpMethod.Post,
            $"{ApiBase}/repos/{owner}/{repo}/git/trees", payload, ct);
        Track(r);
        return r.Ok && r.Doc != null && r.Doc.RootElement.TryGetProperty("sha", out var s) ? s.GetString() : null;
    }

    /// <summary>Creates a commit and returns its SHA.</summary>
    public async Task<string?> CreateCommitAsync(string owner, string repo, string message,
        string treeSha, string? parentSha, CancellationToken ct = default)
    {
        var author = new Dictionary<string, object?>
        {
            ["name"]  = _auth.Account?.Name ?? _auth.Account?.Login ?? "Kmila user",
            ["email"] = $"{_auth.Account?.Login ?? "kmila"}@users.noreply.github.com",
            ["date"]  = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var payload = new Dictionary<string, object?>
        {
            ["message"] = message,
            ["tree"]    = treeSha,
            ["author"]  = author
        };
        if (!string.IsNullOrEmpty(parentSha))
            payload["parents"] = new[] { parentSha };

        using var r = await _auth.SendApiAsync(HttpMethod.Post,
            $"{ApiBase}/repos/{owner}/{repo}/git/commits", payload, ct);
        Track(r);
        return r.Ok && r.Doc != null && r.Doc.RootElement.TryGetProperty("sha", out var s) ? s.GetString() : null;
    }

    /// <summary>Reads a recursive tree (path → blob sha) at <paramref name="treeSha"/>.</summary>
    public async Task<List<VcTreeEntry>?> GetTreeAsync(string owner, string repo, string treeSha, CancellationToken ct = default)
    {
        using var r = await _auth.SendApiAsync(HttpMethod.Get,
            $"{ApiBase}/repos/{owner}/{repo}/git/trees/{treeSha}?recursive=1", null, ct);
        Track(r);
        if (!r.Ok || r.Doc == null || !r.Doc.RootElement.TryGetProperty("tree", out var arr)) return null;
        var list = new List<VcTreeEntry>();
        foreach (var e in arr.EnumerateArray())
        {
            var type = e.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type != "blob") continue; // skip subtree markers
            var path = e.TryGetProperty("path", out var p) ? p.GetString() : null;
            var sha  = e.TryGetProperty("sha", out var s) ? s.GetString() : null;
            if (path != null && sha != null) list.Add(new VcTreeEntry(path, sha, type!));
        }
        return list;
    }

    /// <summary>The tree SHA of a given commit.</summary>
    public async Task<string?> GetCommitTreeShaAsync(string owner, string repo, string commitSha, CancellationToken ct = default)
    {
        using var r = await _auth.SendApiAsync(HttpMethod.Get,
            $"{ApiBase}/repos/{owner}/{repo}/git/commits/{commitSha}", null, ct);
        Track(r);
        return r.Ok && r.Doc != null && r.Doc.RootElement.TryGetProperty("tree", out var tr)
            && tr.TryGetProperty("sha", out var s) ? s.GetString() : null;
    }

    // ─── History / repos ─────────────────────────────────────────

    /// <summary>Recent commits on <paramref name="branch"/> (newest first).</summary>
    public async Task<List<VcCommit>?> GetCommitsAsync(string owner, string repo, string branch, int perPage = 30, CancellationToken ct = default)
    {
        using var r = await _auth.SendApiAsync(HttpMethod.Get,
            $"{ApiBase}/repos/{owner}/{repo}/commits?sha={Uri.EscapeDataString(branch)}&per_page={perPage}", null, ct);
        Track(r);
        if (!r.Ok || r.Doc == null || r.Doc.RootElement.ValueKind != JsonValueKind.Array) return null;
        var list = new List<VcCommit>();
        foreach (var e in r.Doc.RootElement.EnumerateArray())
        {
            var sha = e.TryGetProperty("sha", out var s) ? s.GetString() ?? "" : "";
            var htmlUrl = e.TryGetProperty("html_url", out var h) ? h.GetString() : null;
            string msg = "", authorName = "";
            DateTime date = DateTime.MinValue;
            if (e.TryGetProperty("commit", out var commit))
            {
                if (commit.TryGetProperty("message", out var m)) msg = m.GetString() ?? "";
                if (commit.TryGetProperty("author", out var a))
                {
                    if (a.TryGetProperty("name", out var an)) authorName = an.GetString() ?? "";
                    if (a.TryGetProperty("date", out var ad) && ad.TryGetDateTime(out var d)) date = d;
                }
            }
            list.Add(new VcCommit(sha, msg, authorName, date, htmlUrl));
        }
        return list;
    }

    /// <summary>The signed-in user's repositories (for the link picker), newest first.</summary>
    public async Task<List<VcRepoInfo>?> ListReposAsync(CancellationToken ct = default)
    {
        using var r = await _auth.SendApiAsync(HttpMethod.Get,
            $"{ApiBase}/user/repos?per_page=100&sort=updated&affiliation=owner", null, ct);
        Track(r);
        if (!r.Ok || r.Doc == null || r.Doc.RootElement.ValueKind != JsonValueKind.Array) return null;
        var list = new List<VcRepoInfo>();
        foreach (var e in r.Doc.RootElement.EnumerateArray())
            list.Add(ParseRepo(e));
        return list;
    }

    /// <summary>Creates a repo under the signed-in account (auto_init so a branch exists).</summary>
    public async Task<VcRepoInfo?> CreateRepoAsync(string name, string? description, bool isPrivate, CancellationToken ct = default)
    {
        using var r = await _auth.SendApiAsync(HttpMethod.Post, $"{ApiBase}/user/repos",
            new Dictionary<string, object?>
            {
                ["name"] = name,
                ["description"] = description ?? "",
                ["private"] = isPrivate,
                ["auto_init"] = true
            }, ct);
        Track(r);
        return r.Ok && r.Doc != null ? ParseRepo(r.Doc.RootElement) : null;
    }

    private static VcRepoInfo ParseRepo(JsonElement e)
    {
        var name = e.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        var owner = e.TryGetProperty("owner", out var o) && o.TryGetProperty("login", out var l) ? l.GetString() ?? "" : "";
        var def = e.TryGetProperty("default_branch", out var d) ? d.GetString() ?? "main" : "main";
        var priv = e.TryGetProperty("private", out var p) && p.GetBoolean();
        return new VcRepoInfo(owner, name, def, priv);
    }

    private void Track(GitHubApiResult r) => LastRateLimited = r.RateLimited;
}
