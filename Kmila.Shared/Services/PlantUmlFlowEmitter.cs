using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Kmila.Shared.Services;

/// <summary>
/// Emits a PlantUML activity diagram from <see cref="ProgramBuilder.Parser"/>'s
/// Entities collection. Parallel walker to the Mermaid renderer in
/// FlowDiagramViewer.razor — kept independent so the on-screen Mermaid path
/// is untouched. Output is a <c>.puml</c> text payload that opens in PlantUML
/// tooling (VS Code plugin, IntelliJ, draw.io PlantUML import, plantuml.jar,
/// kroki, etc.). Reflection-based AST walk so we don't take a typed
/// dependency on Interpreter repository classes.
/// </summary>
public static class PlantUmlFlowEmitter
{
    /// <summary>
    /// Walk <paramref name="entities"/> and return PlantUML activity-diagram
    /// source. Returns an empty string when there are no entities so callers
    /// can detect "nothing to export" without an exception.
    /// </summary>
    public static string Emit(IEnumerable? entities)
    {
        if (entities == null) return string.Empty;
        var list = new List<object>();
        foreach (var e in entities) if (e != null) list.Add(e);
        if (list.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("title Kmila — Control flow");
        sb.AppendLine("skinparam shadowing false");
        sb.AppendLine("skinparam activity {");
        sb.AppendLine("  BackgroundColor #f1f5f9");
        sb.AppendLine("  BorderColor #64748b");
        sb.AppendLine("  DiamondBackgroundColor #fae8ff");
        sb.AppendLine("  DiamondBorderColor #a855f7");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("start");

        foreach (var entity in list)
        {
            try { EmitEntity(sb, entity, indent: 0); }
            catch (Exception ex)
            {
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $":⚠ render error: {EscapeLabel(ex.Message)};");
            }
        }

        sb.AppendLine("stop");
        sb.AppendLine("@enduml");
        return sb.ToString();
    }

    private static void EmitEntity(StringBuilder sb, object entity, int indent)
    {
        string entityName = TryGetString(entity, "Name") ?? "(entity)";
        var arch = TryGetProp(entity, "Architecture");
        string archName = arch != null ? (TryGetString(arch, "Name") ?? "(arch)") : "(arch)";

        var pad = Pad(indent);
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}partition \"entity {EscapeQuoted(entityName)} / architecture {EscapeQuoted(archName)}\" {{");

        var archTasks = arch != null ? TryGetProp(arch, "Tasks") as IEnumerable : null;
        var taskList = new List<object>();
        if (archTasks != null) foreach (var t in archTasks) if (t != null) taskList.Add(t);

        // Multiple concurrent tasks under an architecture run in parallel —
        // map to PlantUML fork/fork again/end fork so the diagram shows them
        // as siblings rather than a sequential chain. Single-task arches skip
        // the fork wrapper for a cleaner output.
        if (taskList.Count > 1)
        {
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}  fork");
            for (int i = 0; i < taskList.Count; i++)
            {
                if (i > 0) sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}  fork again");
                EmitArchTask(sb, taskList[i], i + 1, indent + 2);
            }
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}  end fork");
        }
        else if (taskList.Count == 1)
        {
            EmitArchTask(sb, taskList[0], 1, indent + 1);
        }

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}}}");
    }

    private static void EmitArchTask(StringBuilder sb, object task, int idx, int indent)
    {
        var typeName = task.GetType().Name;
        var pad = Pad(indent);

        if (typeName == "Process")
        {
            var label = new StringBuilder($"process #{idx}");
            try
            {
                var sens = TryGetProp(task, "SensitiveList") as IEnumerable;
                if (sens != null)
                {
                    var names = new List<string>();
                    foreach (var v in sens)
                    {
                        var n = TryGetString(v, "Name");
                        if (!string.IsNullOrEmpty(n)) names.Add(n!);
                    }
                    if (names.Count > 0)
                        label.Append(System.Globalization.CultureInfo.InvariantCulture, $" (sensitive to: {string.Join(", ", names)})");
                }
            }
            catch { }

            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}partition \"{EscapeQuoted(label.ToString())}\" {{");
            var body = TryGetProp(task, "Tasks") as IEnumerable;
            if (body != null)
            {
                int j = 0;
                foreach (var sub in body)
                {
                    j++;
                    EmitSequential(sb, sub, j, indent + 1);
                }
            }
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}}}");
            return;
        }

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}:{EscapeLabel(DescribeConcurrent(task, typeName))};");
    }

    private static void EmitSequential(StringBuilder sb, object task, int idx, int indent)
    {
        if (task == null) return;
        var typeName = task.GetType().Name;
        var pad = Pad(indent);

        if (typeName == "IfStructure")
        {
            EmitIf(sb, task, indent);
            return;
        }

        if (typeName == "CaseStructure")
        {
            EmitCase(sb, task, indent);
            return;
        }

        if (typeName.EndsWith("LoopStructure", System.StringComparison.Ordinal) || typeName == "GenerateBlock")
        {
            EmitLoop(sb, task, typeName, indent);
            return;
        }

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}:{EscapeLabel(DescribeConcurrent(task, typeName))};");
    }

    private static void EmitIf(StringBuilder sb, object task, int indent)
    {
        var pad = Pad(indent);
        var branches = new List<object>();
        try
        {
            var brs = TryGetProp(task, "Branches") as IEnumerable;
            if (brs != null) foreach (var br in brs) if (br != null) branches.Add(br);
        }
        catch { }

        if (branches.Count == 0)
        {
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}:if (empty);");
            return;
        }

        bool first = true;
        bool hasElse = false;
        foreach (var br in branches)
        {
            bool isElse = false;
            try { var v = TryGetProp(br, "IsElse"); if (v is bool b) isElse = b; } catch { }
            if (isElse) hasElse = true;

            var cond = TryGetProp(br, "Condition");
            var rawCond = cond != null ? TryGetString(cond, "RawData") : null;
            var condLabel = string.IsNullOrWhiteSpace(rawCond) ? "?" : Truncate(rawCond!.Trim(), 48);

            if (first)
            {
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}if (\"{EscapeQuoted(condLabel)}\") then (yes)");
                first = false;
            }
            else if (isElse)
            {
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}else (no)");
            }
            else
            {
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}elseif (\"{EscapeQuoted(condLabel)}\") then (yes)");
            }

            var body = TryGetProp(br, "Tasks") as IEnumerable;
            if (body != null)
            {
                int j = 0;
                foreach (var t in body) { j++; EmitSequential(sb, t, j, indent + 1); }
            }
        }

        if (!hasElse) sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}else (no)");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}endif");
    }

    private static void EmitCase(StringBuilder sb, object task, int indent)
    {
        var pad = Pad(indent);
        var sel = TryGetString(task, "SelectorExpression")
               ?? TryGetString(task, "Selector")
               ?? TryGetString(task, "Expression")
               ?? TryGetString(task, "RawData")
               ?? "?";
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}switch (\"{EscapeQuoted(Truncate(sel.Trim(), 48))}\")");

        var cases = TryGetProp(task, "Branches") as IEnumerable
                 ?? TryGetProp(task, "Cases") as IEnumerable;
        if (cases != null)
        {
            int c = 0;
            foreach (var ce in cases)
            {
                c++;
                var label = TryGetString(ce, "Label") ?? $"when #{c}";
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}case (\"{EscapeQuoted(Truncate(label.Trim(), 32))}\")");
                var body = TryGetProp(ce, "Tasks") as IEnumerable;
                if (body != null)
                {
                    int j = 0;
                    foreach (var t in body) { j++; EmitSequential(sb, t, j, indent + 1); }
                }
            }
        }
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}endswitch");
    }

    private static void EmitLoop(StringBuilder sb, object task, string typeName, int indent)
    {
        var pad = Pad(indent);
        var keyword = typeName switch
        {
            "ForLoopStructure"    => "for",
            "WhileLoopStructure"  => "while",
            "SimpleLoopStructure" => "loop",
            "GenerateBlock"       => "generate",
            _                     => "loop"
        };
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}repeat :{EscapeLabel(keyword)};");
        var body = TryGetProp(task, "Tasks") as IEnumerable;
        if (body != null)
        {
            int j = 0;
            foreach (var t in body) { j++; EmitSequential(sb, t, j, indent + 1); }
        }
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{pad}repeat while (\"{EscapeQuoted(keyword)} iter\")");
    }

    private static string DescribeConcurrent(object task, string typeName)
    {
        var raw = TryGetString(task, "RawData");
        if (!string.IsNullOrWhiteSpace(raw)) return Truncate(raw!.Trim());

        if (typeName == "ConditionalAssignment")
        {
            var target = TryGetString(task, "TargetName") ?? "?";
            var parts = new List<string>();
            try
            {
                var brs = TryGetProp(task, "Branches") as IEnumerable;
                if (brs != null)
                {
                    foreach (var br in brs)
                    {
                        var v = TryGetString(br, "ValueExpression") ?? "?";
                        var c = TryGetString(br, "ConditionExpression");
                        parts.Add(string.IsNullOrEmpty(c) ? v : $"{v} when {c}");
                    }
                }
            }
            catch { }
            return Truncate($"{target} <= " + (parts.Count > 0 ? string.Join(" else ", parts) : "(?)"));
        }

        if (typeName == "SelectedAssignment")
        {
            var target   = TryGetString(task, "TargetName") ?? "?";
            var selector = TryGetString(task, "SelectorExpression") ?? "?";
            var n        = 0;
            try
            {
                var brs = TryGetProp(task, "Branches") as IEnumerable;
                if (brs != null) foreach (var _ in brs) n++;
            }
            catch { }
            return Truncate($"with {selector} select {target} <= … ({n} branches)");
        }

        return typeName;
    }

    private static string? TryGetString(object? obj, string name)
    {
        if (obj == null) return null;
        try
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(obj) as string;
        }
        catch { return null; }
    }

    private static object? TryGetProp(object? obj, string name)
    {
        if (obj == null) return null;
        try
        {
            return obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);
        }
        catch { return null; }
    }

    private static string Truncate(string s, int max = 64)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }

    /// <summary>
    /// Escapes a free-form label used inside <c>:label;</c> action syntax.
    /// PlantUML treats <c>;</c> as the terminator and <c>\n</c> as a newline
    /// inside the label, so we strip the trailing <c>;</c> (carried over
    /// from raw VHDL lines) and convert hard line breaks to <c>\n</c>.
    /// </summary>
    private static string EscapeLabel(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Replace("\r", "", System.StringComparison.Ordinal).Replace("\n", " ", System.StringComparison.Ordinal).TrimEnd(';').Trim();
        return s.Replace(";", ",", System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Escapes text destined for a double-quoted PlantUML token (partition
    /// title, condition string in <c>if (\"…\") then …</c>, case key).
    /// Only quotes need handling — parens and other glyphs render fine
    /// inside the quoted form.
    /// </summary>
    private static string EscapeQuoted(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\r", "", System.StringComparison.Ordinal).Replace("\n", " ", System.StringComparison.Ordinal).Replace("\"", "'", System.StringComparison.Ordinal).Trim();
    }

    private static string Pad(int indent) => indent <= 0 ? string.Empty : new string(' ', indent * 2);
}
