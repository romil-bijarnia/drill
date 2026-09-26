namespace Reps;

public static class TextDiff
{
    /// Collapses whitespace so that formatting is not what recall is graded on: any run of
    /// whitespace between two identifier characters becomes one space, every other run is
    /// dropped. `var x=1;` and `var x = 1;` normalise to the same string.
    public static string Normalize(string code)
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
    /// position after trimming, marking each pair as same or different.
    public static IReadOnlyList<(string Expected, string Actual, bool Same)> LineReport(string expected, string actual)
    {
        var left = expected.Replace("\r\n", "\n").Split('\n');
        var right = actual.Replace("\r\n", "\n").Trim('\n').Split('\n');
        var rows = new List<(string, string, bool)>();
        for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            var l = i < left.Length ? left[i] : "";
            var r = i < right.Length ? right[i] : "";
            rows.Add((l, r, Normalize(l) == Normalize(r)));
        }
        return rows;
    }
}
