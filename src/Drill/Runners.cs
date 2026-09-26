using System.Diagnostics;
using System.Text;

namespace Drill;

/// One entry point for blank mode and `drill verify`: compile and test an attempt in the
/// snippet's language. C# runs in-process (Roslyn); Python, C and assembly run as child
/// processes with a generated harness that prints one `__R__` line per test.
public static class Runner
{
    public const string Marker = "__R__";

    public static Task<EvalResult> EvaluateAsync(Snippet snippet, string code, TimeSpan timeout) => snippet.Language switch
    {
        Language.CSharp => Evaluator.RunAsync(code, snippet.Tests, timeout),
        Language.Python => PythonRunner.RunAsync(code, snippet.Tests, timeout),
        Language.C => CRunner.RunAsync(code, null, snippet.Decl, snippet.Tests, timeout),
        Language.Asm => CRunner.RunAsync(null, code, snippet.Decl, snippet.Tests, timeout),
        _ => throw new ArgumentOutOfRangeException(nameof(snippet)),
    };

    /// Which tool serves each language, or null when it is missing from this machine.
    public static string? Tool(Language language) => language switch
    {
        Language.CSharp => "roslyn (in-process)",
        Language.Python => Toolchain.Find("python3"),
        Language.C or Language.Asm => Toolchain.Find("cc"),
        _ => null,
    };

    public static bool Available(Language language) => Tool(language) is not null;

    /// Turns harness output into outcomes: the nth `__R__` line answers the nth test.
    internal static EvalResult FromOutput(IReadOnlyList<SnippetTest> tests, string stdout, string stderr, int exitCode, bool timedOut, TimeSpan timeout)
    {
        var results = stdout.Replace("\r\n", "\n").Split('\n')
            .Where(l => l.StartsWith(Marker, StringComparison.Ordinal))
            .Select(l => l[Marker.Length..])
            .ToList();
        var outcomes = new List<TestOutcome>();
        for (var i = 0; i < tests.Count; i++)
        {
            string actual;
            if (i < results.Count)
            {
                actual = results[i];
            }
            else if (timedOut)
            {
                actual = $"timed out after {timeout.TotalSeconds:0}s";
            }
            else if (exitCode != 0)
            {
                actual = $"program stopped ({DescribeExit(exitCode)}){Trail(stderr)}";
            }
            else
            {
                actual = "no result printed";
            }
            outcomes.Add(new TestOutcome(tests[i], actual == tests[i].Expect, actual));
        }
        return new EvalResult(true, [], outcomes);
    }

    private static string DescribeExit(int code) => code switch
    {
        139 or -11 or 11 => "segmentation fault",
        134 or -6 or 6 => "abort",
        136 or -8 or 8 => "floating point exception",
        _ when code > 128 => $"signal {code - 128}",
        _ => $"exit code {code}",
    };

    private static string Trail(string stderr)
    {
        var line = stderr.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0);
        return line is null ? "" : $": {line}";
    }
}

internal static class Toolchain
{
    private static readonly Dictionary<string, string?> Cache = new();

    public static string? Find(string program)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(program, out var cached))
            {
                return cached;
            }
            var directories = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
                .Concat(["/opt/homebrew/bin", "/usr/local/bin", "/usr/bin"]);
            var found = directories
                .Select(d => Path.Combine(d, program))
                .FirstOrDefault(p => File.Exists(p) && !Directory.Exists(p));
            Cache[program] = found;
            return found;
        }
    }
}

internal sealed record ProcessResult(int ExitCode, string Stdout, string Stderr, bool TimedOut);

internal static class Processes
{
    public static async Task<ProcessResult> RunAsync(string program, IEnumerable<string> arguments, string workingDirectory, TimeSpan timeout)
    {
        var info = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }
        info.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        info.Environment["PYTHONIOENCODING"] = "utf-8";
        using var process = new Process { StartInfo = info };
        process.Start();
        process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var cancel = new CancellationTokenSource(timeout);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(cancel.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
        }
        var output = await stdout;
        var errors = await stderr;
        return new ProcessResult(timedOut ? -1 : process.ExitCode, output, errors, timedOut);
    }

    public static string ScratchDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"drill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static void Cleanup(string directory)
    {
        try { Directory.Delete(directory, recursive: true); } catch { /* scratch */ }
    }
}

/// Python: the attempt followed by one guarded print per test. `None` prints as an
/// empty string; everything else through str().
public static class PythonRunner
{
    public static async Task<EvalResult> RunAsync(string code, IReadOnlyList<SnippetTest> tests, TimeSpan timeout)
    {
        var python = Toolchain.Find("python3") ?? throw new DrillException("python3 was not found on PATH");
        var scratch = Processes.ScratchDirectory();
        try
        {
            var script = new StringBuilder();
            script.Append(code.Replace("\r\n", "\n")).Append("\n\n");
            script.Append("def __reps_fmt(v):\n    return '' if v is None else str(v)\n\n");
            script.Append("def raises(fn):\n    try:\n        fn()\n        return 'no throw'\n    except BaseException as e:\n        return type(e).__name__\n\n");
            foreach (var test in tests)
            {
                script.Append("try:\n")
                    .Append("    print('").Append(Runner.Marker).Append("' + __reps_fmt(").Append(test.Call).Append("), flush=True)\n")
                    .Append("except BaseException as __e:\n")
                    .Append("    print('").Append(Runner.Marker).Append("threw ' + type(__e).__name__ + ': ' + str(__e), flush=True)\n");
            }
            var path = Path.Combine(scratch, "attempt.py");
            await File.WriteAllTextAsync(path, script.ToString());
            var run = await Processes.RunAsync(python, ["-I", path], scratch, timeout);
            var hasResults = run.Stdout.Contains(Runner.Marker, StringComparison.Ordinal);
            if (!hasResults && (run.ExitCode != 0 || run.TimedOut))
            {
                return new EvalResult(false, [run.TimedOut ? $"timed out after {timeout.TotalSeconds:0}s" : Tail(run.Stderr, 6)], []);
            }
            return Runner.FromOutput(tests, run.Stdout, run.Stderr, run.ExitCode, run.TimedOut, timeout);
        }
        finally
        {
            Processes.Cleanup(scratch);
        }
    }

    internal static string Tail(string text, int lines)
    {
        var all = text.Replace("\r\n", "\n").Split('\n').Where(l => l.Trim().Length > 0).ToList();
        return string.Join("\n", all.Skip(Math.Max(0, all.Count - lines)));
    }
}

/// C, and ARM64 assembly linked against C: a harness with the standard headers, the
/// attempt (C source, or `decl` prototypes for an assembly file), and a main that prints
/// each test expression through a _Generic dispatcher so no test needs a format string.
public static class CRunner
{
    private const string Prelude = """
        #include <stdio.h>
        #include <stdlib.h>
        #include <string.h>
        #include <stdbool.h>
        #include <stdint.h>
        #include <limits.h>
        #include <math.h>
        #include <ctype.h>

        """;

    private const string Printers = """

        static void __reps_int(long long v) { printf("__R__%lld\n", v); }
        static void __reps_uint(unsigned long long v) { printf("__R__%llu\n", v); }
        static void __reps_double(double v) { printf("__R__%g\n", v); }
        static void __reps_str(const char *s) { printf("__R__%s\n", s ? s : ""); }
        static void __reps_char(char c) { printf("__R__%c\n", c); }
        static void __reps_bool(_Bool b) { printf("__R__%s\n", b ? "true" : "false"); }
        static void __reps_ptr(const void *p) { printf("__R__%s\n", p ? "(pointer)" : "(null)"); }
        #define __REPS_PRINT(x) _Generic((x), \
            _Bool: __reps_bool, char: __reps_char, \
            signed char: __reps_int, short: __reps_int, int: __reps_int, long: __reps_int, long long: __reps_int, \
            unsigned char: __reps_uint, unsigned short: __reps_uint, unsigned: __reps_uint, unsigned long: __reps_uint, unsigned long long: __reps_uint, \
            float: __reps_double, double: __reps_double, \
            char *: __reps_str, const char *: __reps_str, \
            default: __reps_ptr)(x)

        int main(void)
        {
            setvbuf(stdout, NULL, _IONBF, 0);

        """;

    public static async Task<EvalResult> RunAsync(string? cSource, string? asmSource, string decl, IReadOnlyList<SnippetTest> tests, TimeSpan timeout)
    {
        var cc = Toolchain.Find("cc") ?? throw new DrillException("cc was not found; install the Xcode command line tools (xcode-select --install)");
        var scratch = Processes.ScratchDirectory();
        try
        {
            var harness = new StringBuilder(Prelude);
            if (asmSource is not null)
            {
                harness.Append(decl).Append('\n');
            }
            else
            {
                harness.Append(cSource!.Replace("\r\n", "\n")).Append('\n');
            }
            harness.Append(Printers);
            foreach (var test in tests)
            {
                harness.Append("    __REPS_PRINT(").Append(test.Call).Append(");\n");
            }
            harness.Append("    return 0;\n}\n");
            var harnessPath = Path.Combine(scratch, "harness.c");
            await File.WriteAllTextAsync(harnessPath, harness.ToString());
            var inputs = new List<string> { harnessPath };
            if (asmSource is not null)
            {
                var asmPath = Path.Combine(scratch, "attempt.s");
                await File.WriteAllTextAsync(asmPath, asmSource.Replace("\r\n", "\n") + "\n");
                inputs.Add(asmPath);
            }
            var binary = Path.Combine(scratch, "attempt");
            var compile = await Processes.RunAsync(cc, ["-std=c11", "-O0", "-Wall", "-Wno-unused-function", "-o", binary, .. inputs, "-lm"], scratch, TimeSpan.FromSeconds(60));
            if (compile.ExitCode != 0)
            {
                return new EvalResult(false, Errors(compile.Stderr, harnessPath), []);
            }
            var run = await Processes.RunAsync(binary, [], scratch, timeout);
            return Runner.FromOutput(tests, run.Stdout, run.Stderr, run.ExitCode, run.TimedOut, timeout);
        }
        finally
        {
            Processes.Cleanup(scratch);
        }
    }

    /// Compiler messages, with the scratch path stripped and harness line numbers shifted
    /// so they match the attempt (the prelude occupies the first lines of the file).
    private static List<string> Errors(string stderr, string harnessPath)
    {
        var preludeLines = Prelude.Count(c => c == '\n');
        var errors = new List<string>();
        foreach (var raw in stderr.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (!line.Contains("error:", StringComparison.Ordinal) && !line.Contains("undefined symbol", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (line.StartsWith(harnessPath, StringComparison.Ordinal))
            {
                var rest = line[harnessPath.Length..];
                var parts = rest.Split(':', 4);
                if (parts.Length >= 4 && int.TryParse(parts[1], out var number))
                {
                    line = $"line {number - preludeLines}:{parts[3]}";
                }
                else
                {
                    line = rest.TrimStart(':');
                }
            }
            errors.Add(line);
            if (errors.Count == 6)
            {
                break;
            }
        }
        if (errors.Count == 0)
        {
            errors.Add(PythonRunner.Tail(stderr, 4));
        }
        return errors;
    }
}
