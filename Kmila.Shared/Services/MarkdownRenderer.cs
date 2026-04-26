using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace Kmila.Shared.Services;

/// <summary>
/// Central Markdown → HTML pipeline. Uses Markdig with the "advanced"
/// extension set so GitHub-flavored features render correctly — pipe tables,
/// fenced code blocks (with whitespace preserved for ASCII art), task lists,
/// autolinks, footnotes, definition lists, and emoji shortcodes.
/// </summary>
/// <remarks>
/// The pipeline is built once and cached. Output is always a <see cref="MarkupString"/>
/// so callers can drop it directly into a Razor expression:
/// <code>
///     @MarkdownRenderer.Render(_body)
/// </code>
/// Content is trusted (bundled lesson files + user project files on the local
/// machine). Markdig's default pipeline strips none of the tags, which is
/// fine for our single-user desktop/web model; revisit if the curriculum
/// ever starts loading Markdown from a remote source.
/// </remarks>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()   // pipe tables, task lists, autolinks, footnotes, DL, emphasis-extras, abbreviations, citations, custom containers, grid tables, figures, math, diagrams passthrough, emoji/smiley, …
        .UseSoftlineBreakAsHardlineBreak() // match GitHub rendering — a newline inside a paragraph becomes <br>
        .Build();

    /// <summary>
    /// Matches Markdig's output for ```mermaid fenced blocks. We rewrite
    /// those into the container that mermaid.js auto-detects
    /// (<c>&lt;pre class="mermaid"&gt;…&lt;/pre&gt;</c>) so lesson authors can
    /// drop diagrams in alongside regular Markdown. Using a non-greedy match
    /// is safe because Markdig emits the surrounding tags on their own lines.
    /// </summary>
    private static readonly Regex _mermaidFence = new(
        @"<pre><code class=""language-mermaid"">(?<body>.*?)</code></pre>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>
    /// Converts Markdown source to a <see cref="MarkupString"/> that Razor
    /// will render as HTML without re-escaping. ```mermaid fenced blocks are
    /// rewritten so the bundled mermaid.js can auto-initialize them.
    /// </summary>
    public static MarkupString Render(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return default;
        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        html = _mermaidFence.Replace(html,
            m => $"<pre class=\"mermaid\">{System.Net.WebUtility.HtmlDecode(m.Groups["body"].Value)}</pre>");
        return (MarkupString)html;
    }
}
