namespace Reps;

public static class TextDiff
{
    /// Collapses formatting that recall should not grade. For C and C#, any run of
    /// whitespace between two identifier characters becomes one space and every other
    /// run is dropped, so `var x=1;` and `var x = 1;` match. Python and assembly are
    /// line-based: indentation depth and line breaks are kept, comments and trailing
    /// space are not.
    public static string Normalize(string code, Language language = Language.CSharp) => language switch
    {
        Language.Python => NormalizeLines(code, commentStart: "#", keepIndent: true),
        Language.Asm => NormalizeLines(code, commentStart: "//", keepIndent: false),
        _ => NormalizeTokens(code),
    };

    private static string NormalizeTokens(string code)
    {
        var text = code.Replace("\r\n", "\n");
        var output = new System.Text.StringBuilder(text.Length);
        var index = 0;
        while (index < text.Length)
        {
            if (!char.IsWhiteSpace(text[index]))
            {
                output.Append(text[index]);
                index++;
                continue;
            }
            var next = index;
            while (next < text.Length && char.IsWhiteSpace(text[next]))
            {
                next++;
            }
            var previous = output.Length > 0 ? output[^1] : '\0';
            var following = next < text.Length ? text[next] : '\0';
            if (IsWord(previous) && IsWord(following))
            {
                output.Append(' ');
            }
            index = next;
        }
        return output.ToString();
    }

    private static string NormalizeLines(string code, string commentStart, bool keepIndent)
    {
        var lines = new List<string>();
        foreach (var raw in code.Replace("\r\n", "\n").Replace("\t", "    ").Split('\n'))
        {
            var line = raw;
            var comment = line.IndexOf(commentStart, StringComparison.Ordinal);
            if (comment >= 0 && !InsideQuotes(line, comment))
            {
                line = line[..comment];
            }
            if (line.Trim().Length == 0)
            {
                continue;
            }
            var indent = keepIndent ? line.Length - line.TrimStart().Length : 0;
            var body = string.Join(" ", line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            body = body.Replace(" ,", ",").Replace(", ", ",").Replace(" (", "(").Replace("( ", "(").Replace(" )", ")");
            lines.Add(new string(' ', indent) + body);
        }
        return string.Join("\n", lines);
    }

    private static bool InsideQuotes(string line, int index)
    {
        var quotes = 0;
        for (var i = 0; i < index; i++)
        {
            if (line[i] is '"' or '\'') quotes++;
        }
        return quotes % 2 == 1;
    }

    private static bool IsWord(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '@';

    public static int Levenshtein(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }

    /// Line-level comparison for the recall report: pairs expected and typed lines by
    /// position, marking each pair as same or different under the language's rules.
    public static IReadOnlyList<(string Expected, string Actual, bool Same)> LineReport(string expected, string actual, Language language = Language.CSharp)
    {
        var left = expected.Replace("\r\n", "\n").Split('\n');
        var right = actual.Replace("\r\n", "\n").Trim('\n').Split('\n');
        var rows = new List<(string, string, bool)>();
        for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            var l = i < left.Length ? left[i] : "";
            var r = i < right.Length ? right[i] : "";
            rows.Add((l, r, Normalize(l, language) == Normalize(r, language)));
        }
        return rows;
    }
}
