using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Reps;

public sealed record TestOutcome(SnippetTest Test, bool Passed, string Actual);

public sealed record EvalResult(bool Compiled, IReadOnlyList<string> CompileErrors, IReadOnlyList<TestOutcome> Outcomes)
{
    public bool AllPassed => Compiled && Outcomes.Count > 0 && Outcomes.All(o => o.Passed);
}

/// Compiles an attempt in-process with Roslyn scripting and evaluates each test call
/// against it. A test passes when the call's result, formatted invariantly, equals the
/// expected string. No `dotnet build`, so a run takes well under a second after warm-up.
public static class Evaluator
{
    private static readonly Lazy<ScriptOptions> Options = new(BuildOptions);

    // Roslyn scripting's first-time initialisation is not safe to race; the trainer never
    // evaluates concurrently, but the test suite does, so serialise to be certain.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static ScriptOptions BuildOptions()
    {
        var references = new List<Assembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            typeof(Console).Assembly,
            typeof(System.Text.RegularExpressions.Regex).Assembly,
            typeof(System.Collections.Concurrent.ConcurrentDictionary<,>).Assembly,
            typeof(System.Numerics.BigInteger).Assembly,
        };
        foreach (var name in new[] { "System.Runtime", "netstandard", "System.Collections", "System.Linq", "System.Threading", "System.Threading.Tasks" })
        {
            try
            {
                references.Add(Assembly.Load(new AssemblyName(name)));
            }
            catch
            {
                // Facade not present on this runtime; the typeof references above cover it.
            }
        }
        return ScriptOptions.Default
            .WithReferences(references.Distinct())
            .WithImports("System", "System.Linq", "System.Collections.Generic", "System.Threading.Tasks",
                "System.Text", "System.Text.RegularExpressions", "System.Globalization", "System.Numerics");
    }

    public static async Task<EvalResult> RunAsync(string code, IReadOnlyList<SnippetTest> tests, TimeSpan timeout)
    {
        await Gate.WaitAsync();
        try
        {
            return await RunUnlockedAsync(code, tests, timeout);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<EvalResult> RunUnlockedAsync(string code, IReadOnlyList<SnippetTest> tests, TimeSpan timeout)
    {
        var script = CSharpScript.Create(code, Options.Value);
        var errors = script.Compile()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Select(d => $"line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage(CultureInfo.InvariantCulture)}")
            .Take(6)
            .ToList();
        if (errors.Count > 0)
        {
            return new EvalResult(false, errors, []);
        }

        ScriptState<object> state;
        try
        {
            state = await WithTimeout(script.RunAsync(), timeout);
        }
        catch (Exception error)
        {
            return new EvalResult(false, [$"running your code threw {Describe(error)}"], []);
        }

        var outcomes = new List<TestOutcome>();
        foreach (var test in tests)
        {
            string actual;
            try
            {
                var next = await WithTimeout(state.ContinueWithAsync<object>(test.Call), timeout);
                actual = Format(next.ReturnValue);
            }
            catch (CompilationErrorException error)
            {
                actual = "did not compile against your code: " + string.Join("; ", error.Diagnostics.Take(2).Select(d => d.GetMessage(CultureInfo.InvariantCulture)));
            }
            catch (TimeoutException)
            {
                actual = $"timed out after {timeout.TotalSeconds:0}s";
            }
            catch (Exception error)
            {
                actual = "threw " + Describe(error);
            }
            outcomes.Add(new TestOutcome(test, actual == test.Expect, actual));
        }
        return new EvalResult(true, [], outcomes);
    }

    private static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        var finished = await Task.WhenAny(task, Task.Delay(timeout));
        if (finished != task)
        {
            throw new TimeoutException();
        }
        return await task;
    }

    private static string Describe(Exception error)
    {
        var inner = error is AggregateException aggregate && aggregate.InnerException is not null ? aggregate.InnerException : error;
        return $"{inner.GetType().Name}: {inner.Message}";
    }

    public static string Format(object? value) => value switch
    {
        null => "",
        bool b => b ? "True" : "False",
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
