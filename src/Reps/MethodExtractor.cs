using System.Text.RegularExpressions;

namespace Reps;

/// Finds static methods in a C# source file and returns each one, dedented, as a
/// candidate snippet. Regex-based on purpose: it only has to be right for ordinary,
/// well-formatted code, and anything it misses can be added by hand.
public static partial class MethodExtractor
{
    [GeneratedRegex(@"^(?<indent>[ \t]*)(?:(?:public|private|internal|protected)\s+)*static\s+(?:async\s+)?[\w<>\[\],\.\?\s]+?\s+(?<name>\w+)\s*(?:<[^>()]*>)?\s*\([^;{}]*\)\s*(?:where[^{;=]*)?(?<open>\{|=>)", RegexOptions.Multiline)]
    private static partial Regex Signature();

    public static IEnumerable<(string Name, string Code)> Extract(string source)
    {
        var text = source.Replace("\r\n", "\n");
        foreach (Match match in Signature().Matches(text))
        {
            var start = match.Index;
            int end;
            if (match.Groups["open"].Value == "{")
            {
                end = MatchBrace(text, match.Index + match.Length - 1);
            }
            else
            {
                end = text.IndexOf(';', match.Index + match.Length);
            }
            if (end < 0)
            {
                continue;
            }
            var block = text[start..(end + 1)];
            var lines = block.Split('\n');
            if (lines.Length > 40)
            {
                continue;
            }
            yield return (match.Groups["name"].Value, Dedent(lines));
        }
    }

    private static int MatchBrace(string text, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}' && --depth == 0) return i;
        }
        return -1;
    }

    private static string Dedent(string[] lines)
    {
        var indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return string.Join("\n", lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart())).Replace("\t", "    ").TrimEnd();
    }
}
