using System.Text;
using Spectre.Console;

namespace Drill;

/// Compile view: what clang makes of a C snippet, shown next to the C, and then your own
/// ARM64 for the same tests. The prototype the harness needs comes from the C source
/// itself, so any C snippet with tests can be taken down to assembly.
public static class CompileView
{
    /// The C source with every function body replaced by a semicolon: what remains are
    /// the includes, types and prototypes the harness needs to call the assembly version.
    public static string Prototypes(string code)
    {
        var text = code.Replace("\r\n", "\n");
        var output = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            var brace = text.IndexOf('{', i);
            if (brace < 0)
            {
                output.Append(text, i, text.Length - i);
                break;
            }
            var j = brace - 1;
            while (j >= 0 && char.IsWhiteSpace(text[j])) j--;
            if (j >= 0 && text[j] == ')')
            {
                var end = MatchingBrace(text, brace);
                output.Append(text, i, j + 1 - i).Append(';');
                i = end + 1;
            }
            else
            {
                output.Append(text, i, brace + 1 - i);
                i = brace + 1;
            }
        }
        var lines = output.ToString().Split('\n')
            .Select(l => l.Replace("static inline ", "").Replace("static ", "").Replace("inline ", "").TrimEnd())
            .Where(l => l.Length > 0);
        return string.Join("\n", lines);
    }

    private static int MatchingBrace(string text, int open)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}' && --depth == 0) return i;
        }
        return text.Length - 1;
    }

    /// clang's ARM64 for the snippet at -O1, with the directives that carry no meaning for
    /// a reader stripped out.
    public static async Task<string> Assemble(string code)
    {
        var cc = Toolchain.Find("cc") ?? throw new DrillException("cc was not found; install the Xcode command line tools");
        var scratch = Processes.ScratchDirectory();
        try
        {
            var source = Path.Combine(scratch, "view.c");
            var listing = Path.Combine(scratch, "view.s");
            await File.WriteAllTextAsync(source, "#include <stdio.h>\n#include <stdlib.h>\n#include <string.h>\n#include <stdbool.h>\n#include <stdint.h>\n#include <limits.h>\n#include <math.h>\n#include <ctype.h>\n\n" + code.Replace("\r\n", "\n") + "\n");
            var result = await Processes.RunAsync(cc, ["-std=c11", "-O1", "-S", "-fno-asynchronous-unwind-tables", "-fno-unwind-tables", "-o", listing, source], scratch, TimeSpan.FromSeconds(60));
            if (result.ExitCode != 0)
            {
                throw new DrillException("clang could not compile the snippet: " + result.Stderr.Split('\n').FirstOrDefault(l => l.Contains("error:")));
            }
            return Clean(await File.ReadAllTextAsync(listing));
        }
        finally
        {
            Processes.Cleanup(scratch);
        }
    }

    public static string Clean(string listing)
    {
        var kept = new List<string>();
        foreach (var raw in listing.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            var comment = line.IndexOf(';');
            if (comment >= 0)
            {
                line = line[..comment].TrimEnd();
            }
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0
                || trimmed.StartsWith(".cfi", StringComparison.Ordinal)
                || trimmed.StartsWith(".build_version", StringComparison.Ordinal)
                || trimmed.StartsWith(".section", StringComparison.Ordinal)
                || trimmed.StartsWith(".subsections_via_symbols", StringComparison.Ordinal)
                || trimmed.StartsWith(".file", StringComparison.Ordinal)
                || trimmed.StartsWith(".loh", StringComparison.Ordinal)
                || trimmed.StartsWith(".private_extern", StringComparison.Ordinal))
            {
                continue;
            }
            kept.Add(line.Replace("\t", "    "));
        }
        return string.Join("\n", kept).Trim('\n');
    }
}
