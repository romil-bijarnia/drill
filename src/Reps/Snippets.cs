namespace Reps;

public enum Mode { Trace, Recall, Blank }

public sealed record SnippetTest(string Call, string Expect);

public sealed record Snippet(
    string Id,
    string Title,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Modes,
    string Spec,
    IReadOnlyList<SnippetTest> Tests,
    string Code,
    string Path)
{
    public bool Supports(Mode mode) => Modes.Contains(mode.ToString().ToLowerInvariant());

    public string FileName => System.IO.Path.GetFileName(Path);
}

/// Reads the snippet bank: one Markdown file per snippet, a small front-matter header
/// between two `---` lines, reference code underneath. Deliberately hand-parsed; the
/// format is ours and a YAML dependency would be more code than this.
public static class SnippetLoader
{
    public static (List<Snippet> Snippets, List<string> Problems) LoadAll(string directory)
    {
        var snippets = new List<Snippet>();
        var problems = new List<string>();
        if (!Directory.Exists(directory))
        {
            return (snippets, problems);
        }
        foreach (var file in Directory.GetFiles(directory, "*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            try
            {
                snippets.Add(Parse(File.ReadAllText(file), file));
            }
            catch (FormatException error)
            {
                problems.Add($"bad snippet: {System.IO.Path.GetFileName(file)} ({error.Message})");
            }
        }
        var duplicates = snippets.GroupBy(s => s.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var id in duplicates)
        {
            problems.Add($"duplicate id {id}");
        }
        return (snippets, problems);
    }

    public static Snippet Parse(string text, string path)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var first = Array.FindIndex(lines, l => l.Trim().Length > 0);
        if (first < 0 || lines[first].Trim() != "---")
        {
            throw new FormatException("missing front matter");
        }
        var second = Array.FindIndex(lines, first + 1, l => l.Trim() == "---");
        if (second < 0)
        {
            throw new FormatException("missing closing ---");
        }

        string? id = null, title = null, spec = null;
        var tags = new List<string>();
        var modes = new List<string>();
        var tests = new List<SnippetTest>();
        string? pendingCall = null;

        foreach (var raw in lines[(first + 1)..second])
        {
            if (raw.Trim().Length == 0)
            {
                continue;
            }
            var line = raw.TrimStart();
            if (line.StartsWith("- call:", StringComparison.Ordinal))
            {
                if (pendingCall is not null)
                {
                    throw new FormatException("test call without expect");
                }
                pendingCall = Unquote(line["- call:".Length..]);
            }
            else if (line.StartsWith("expect:", StringComparison.Ordinal))
            {
                if (pendingCall is null)
                {
                    throw new FormatException("expect without call");
                }
                tests.Add(new SnippetTest(pendingCall, Unquote(line["expect:".Length..])));
                pendingCall = null;
            }
            else if (line.TrimEnd() == "tests:")
            {
                continue;
            }
            else
            {
                var colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    throw new FormatException($"unreadable header line '{raw.Trim()}'");
                }
                var key = line[..colon].Trim();
                var value = line[(colon + 1)..].Trim();
                switch (key)
                {
                    case "id": id = Unquote(value); break;
                    case "title": title = Unquote(value); break;
                    case "spec": spec = Unquote(value); break;
                    case "tags": tags = ParseList(value); break;
                    case "modes": modes = ParseList(value); break;
                    default: break; // unknown keys are ignored so the format can grow
                }
            }
        }
        if (pendingCall is not null)
        {
            throw new FormatException("test call without expect");
        }
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new FormatException("missing id");
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new FormatException("missing title");
        }
        if (modes.Count == 0)
        {
            modes = ["trace", "recall", "blank"];
        }
        var code = string.Join("\n", lines[(second + 1)..]).Trim('\n', ' ', '\t');
        if (code.Length == 0)
        {
            throw new FormatException("no reference code");
        }
        return new Snippet(id!, title!, tags, modes, spec ?? "", tests, code, path);
    }

    private static List<string> ParseList(string value)
    {
        value = value.Trim();
        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            value = value[1..^1];
        }
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Unquote)
            .ToList();
    }

    private static string Unquote(string value)
    {
        value = value.Trim();
        if (value.Length >= 2 && ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"')))
        {
            return value[1..^1];
        }
        return value;
    }

    public static string Template(string id, string title) =>
        $"""
        ---
        id: {id}
        title: {title}
        tags: [todo]
        modes: [trace, recall, blank]
        spec: One line describing what to write.
        tests:
          - call: 'Example(1)'
            expect: '1'
        ---
        public static int Example(int x) => x;

        """;
}
