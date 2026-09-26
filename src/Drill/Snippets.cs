namespace Drill;

public enum Mode { Trace, Recall, Blank, Predict }

public enum Language { CSharp, Python, C, Asm }

public static class Languages
{
    public static readonly Language[] All = [Language.CSharp, Language.Python, Language.C, Language.Asm];

    /// Top of the stack to the bottom: the order `drill down` walks a family.
    public static readonly Language[] Stack = [Language.Python, Language.CSharp, Language.C, Language.Asm];

    public static string Key(this Language language) => language switch
    {
        Language.CSharp => "cs",
        Language.Python => "py",
        Language.C => "c",
        Language.Asm => "asm",
        _ => "cs",
    };

    public static string DisplayName(this Language language) => language switch
    {
        Language.CSharp => "C#",
        Language.Python => "Python",
        Language.C => "C",
        Language.Asm => "ARM64 asm",
        _ => "C#",
    };

    public static string Extension(this Language language) => language switch
    {
        Language.Python => ".py",
        Language.C => ".c",
        Language.Asm => ".s",
        _ => ".cs",
    };

    public static bool TryParse(string? text, out Language language)
    {
        switch (text?.Trim().ToLowerInvariant())
        {
            case "cs" or "csharp" or "c#":
                language = Language.CSharp; return true;
            case "py" or "python":
                language = Language.Python; return true;
            case "c":
                language = Language.C; return true;
            case "asm" or "assembly" or "arm64" or "aarch64":
                language = Language.Asm; return true;
            default:
                language = Language.CSharp; return false;
        }
    }
}

public sealed record SnippetTest(string Call, string Expect);

public sealed record Snippet(
    string Id,
    string Title,
    Language Language,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Modes,
    string Spec,
    string Decl,
    IReadOnlyList<SnippetTest> Tests,
    string Code,
    string Path,
    string Family = "")
{
    public bool Supports(Mode mode) => mode == Mode.Predict
        ? Tests.Count > 0
        : Modes.Contains(mode.ToString().ToLowerInvariant()) && (mode != Mode.Blank || Tests.Count > 0);

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
        foreach (var id in snippets.GroupBy(s => s.Id).Where(g => g.Count() > 1).Select(g => g.Key))
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

        string? id = null, title = null, spec = null, decl = null, lang = null, family = null;
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
                    case "decl": decl = Unquote(value); break;
                    case "family": family = Unquote(value); break;
                    case "lang": lang = Unquote(value); break;
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
        var language = Language.CSharp;
        if (lang is not null && !Languages.TryParse(lang, out language))
        {
            throw new FormatException($"unknown lang '{lang}' (use cs, py, c or asm)");
        }
        if (lang is null)
        {
            var prefix = id!.Split('-')[0];
            if (Languages.TryParse(prefix, out var inferred))
            {
                language = inferred;
            }
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
        return new Snippet(id!, title!, language, tags, modes, spec ?? "", decl ?? "", tests, code, path, family ?? "");
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

    public static string Template(string id, string title, Language language)
    {
        var (example, call, decl) = language switch
        {
            Language.Python => ("def example(x):\n    return x", "example(1)", ""),
            Language.C => ("int example(int x)\n{\n    return x;\n}", "example(1)", ""),
            Language.Asm => (".text\n.globl _example\n.p2align 2\n_example:\n    ret", "example(1)", "decl: 'long example(long);'\n"),
            _ => ("public static int Example(int x) => x;", "Example(1)", ""),
        };
        return $"""
            ---
            id: {id}
            title: {title}
            lang: {language.Key()}
            tags: [todo]
            family:
            modes: [trace, recall, blank]
            spec: One line describing what to write.
            {decl}tests:
              - call: '{call}'
                expect: '1'
            ---
            {example}

            """;
    }
}
