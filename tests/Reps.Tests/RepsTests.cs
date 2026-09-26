using Reps;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

public class SnippetParserTests
{
    private const string Sample = """
        ---
        id: 006
        title: Guarded divide
        tags: [exceptions]
        modes: [trace, recall, blank]
        spec: Divide a by b, throwing DivideByZeroException with message "b was zero" when b is 0.
        tests:
          - call: 'SafeDivide(10, 2)'
            expect: '5'
          - call: '((Func<string>)(() => { try { SafeDivide(1, 0); return "no throw"; } catch (DivideByZeroException e) { return e.Message; } }))()'
            expect: 'b was zero'
        ---
        public static int SafeDivide(int a, int b)
        {
            if (b == 0) throw new DivideByZeroException("b was zero");
            return a / b;
        }
        """;

    [Fact]
    public void ParsesHeaderTestsAndCode()
    {
        var snippet = SnippetLoader.Parse(Sample, "006.md");
        Assert.Equal("006", snippet.Id);
        Assert.Equal("Guarded divide", snippet.Title);
        Assert.Equal(["exceptions"], snippet.Tags);
        Assert.Equal(2, snippet.Tests.Count);
        Assert.Equal("b was zero", snippet.Tests[1].Expect);
        Assert.Contains("try { SafeDivide(1, 0);", snippet.Tests[1].Call);
        Assert.StartsWith("public static int SafeDivide", snippet.Code);
        Assert.EndsWith("}", snippet.Code);
        Assert.True(snippet.Supports(Mode.Blank));
    }

    [Fact]
    public void RejectsFilesWithoutSeparators()
    {
        Assert.Throws<FormatException>(() => SnippetLoader.Parse("just some code", "x.md"));
        Assert.Throws<FormatException>(() => SnippetLoader.Parse("---\nid: 1\ntitle: t\n", "x.md"));
    }
}

public class TextDiffTests
{
    [Fact]
    public void NormaliseIgnoresFormattingButNotTokens()
    {
        Assert.Equal(TextDiff.Normalize("var x = 1;"), TextDiff.Normalize("var x=1;"));
        Assert.Equal(TextDiff.Normalize("foo(a, b)"), TextDiff.Normalize("foo( a ,b )"));
        Assert.NotEqual(TextDiff.Normalize("var x = 1;"), TextDiff.Normalize("varx = 1;"));
        Assert.Equal(0, TextDiff.Levenshtein(TextDiff.Normalize("if (b == 0)\n    throw new X();"), TextDiff.Normalize("if (b == 0) throw new X();")));
    }

    [Fact]
    public void LevenshteinCountsEdits()
    {
        Assert.Equal(3, TextDiff.Levenshtein("kitten", "sitting"));
        Assert.Equal(0, TextDiff.Levenshtein("", ""));
        Assert.Equal(4, TextDiff.Levenshtein("abcd", ""));
    }
}

public class LeitnerTests
{
    [Fact]
    public void PassMovesUpFailResets()
    {
        var today = new DateOnly(2026, 9, 26);
        Assert.Equal((2, today.AddDays(2)), Store.Schedule(1, true, today));
        Assert.Equal((5, today.AddDays(16)), Store.Schedule(5, true, today));
        Assert.Equal((1, today.AddDays(1)), Store.Schedule(4, false, today));
    }

    [Fact]
    public void StoreTracksBoxesAttemptsAndFirstTryRate()
    {
        using var store = Store.InMemory();
        var today = new DateOnly(2026, 9, 26);
        var snippet = SnippetLoader.Parse("---\nid: 001\ntitle: T\n---\nint X() => 1;", "001.md");
        store.Sync([snippet], today);
        Assert.Single(store.Due(today));
        store.RecordAttempt(new AttemptRow("001", Mode.Blank, today.ToDateTime(new TimeOnly(9, 0)), 30, null, false, true));
        store.RecordAttempt(new AttemptRow("001", Mode.Blank, today.ToDateTime(new TimeOnly(9, 5)), 30, null, true, false));
        Assert.Equal(2, store.BlankTriesToday("001", today));
        Assert.Equal(0.0, Store.BlankFirstTryRate(store.AttemptsOn(today)));
        var (box, due) = store.Advance("001", true, today);
        Assert.Equal(2, box);
        Assert.Equal(today.AddDays(2), due);
        Assert.Empty(store.Due(today));
        store.UpsertSession(today);
        Assert.Equal(1, store.SessionDays());
        Assert.Equal(1, store.Streak(today));
    }
}

public class EvaluatorTests
{
    [Fact]
    public async Task RunsTestsAgainstSubmittedCode()
    {
        var tests = new List<SnippetTest> { new("Max(3, 7)", "7"), new("Max(\"apple\", \"pear\")", "pear"), new("Max(2.5, 2.5)", "2.5") };
        var result = await Evaluator.RunAsync("public static T Max<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) >= 0 ? a : b;", tests, TimeSpan.FromSeconds(10));
        Assert.True(result.AllPassed, string.Join("; ", result.CompileErrors.Concat(result.Outcomes.Select(o => o.Actual))));
    }

    [Fact]
    public async Task ReportsCompileErrorsAndWrongAnswers()
    {
        var broken = await Evaluator.RunAsync("public static int Add(int a, int b) => a + ;", [new("Add(1, 2)", "3")], TimeSpan.FromSeconds(10));
        Assert.False(broken.Compiled);
        Assert.NotEmpty(broken.CompileErrors);
        var wrong = await Evaluator.RunAsync("public static int Add(int a, int b) => a - b;", [new("Add(1, 2)", "3")], TimeSpan.FromSeconds(10));
        Assert.True(wrong.Compiled);
        Assert.False(wrong.AllPassed);
        Assert.Equal("-1", wrong.Outcomes[0].Actual);
    }
}

public class SnippetBankTests
{
    private static string BankDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "snippets")))
        {
            directory = directory.Parent;
        }
        return directory is null ? throw new InvalidOperationException("snippets directory not found") : Path.Combine(directory.FullName, "snippets");
    }

    [Fact]
    public async Task EveryReferenceSnippetPassesItsOwnTests()
    {
        var (snippets, problems) = SnippetLoader.LoadAll(BankDirectory());
        Assert.Empty(problems);
        Assert.True(snippets.Count >= 10);
        var failures = new List<string>();
        foreach (var snippet in snippets.Where(s => s.Tests.Count > 0 && Runner.Available(s.Language)))
        {
            var result = await Runner.EvaluateAsync(snippet, snippet.Code, TimeSpan.FromSeconds(30));
            if (!result.AllPassed)
            {
                failures.Add($"{snippet.Id}: {string.Join("; ", result.CompileErrors.Concat(result.Outcomes.Where(o => !o.Passed).Select(o => $"{o.Test.Call} => {o.Actual}")))}");
            }
        }
        Assert.Empty(failures);
    }
}

public class MethodExtractorTests
{
    [Fact]
    public void FindsStaticMethodsWithBlockAndExpressionBodies()
    {
        const string source = """
            namespace Demo;

            public static class Util
            {
                public static int Add(int a, int b) => a + b;

                private static string Greet(string name)
                {
                    if (name.Length == 0) { return "hi"; }
                    return $"hi {name}";
                }

                public void NotStatic() { }
            }
            """;
        var methods = MethodExtractor.Extract(source).ToList();
        Assert.Equal(["Add", "Greet"], methods.Select(m => m.Name));
        Assert.Equal("public static int Add(int a, int b) => a + b;", methods[0].Code);
        Assert.StartsWith("private static string Greet(string name)\n{", methods[1].Code);
        Assert.EndsWith("}", methods[1].Code);
    }
}

public class TextBoxTests
{
    [Fact]
    public void KeepsInitialTextAndJoinsLines()
    {
        var box = new TextBox(["header"], "int x = 1;\nint y = 2;");
        Assert.Equal("int x = 1;\nint y = 2;", box.Text);
    }
}

public class LanguageRunnerTests
{
    [Fact]
    public async Task PythonHarnessRunsTestsAndRaisesHelper()
    {
        if (!Runner.Available(Language.Python)) return;
        var snippet = SnippetLoader.Parse("---\nid: py-900\ntitle: T\nlang: py\ntests:\n  - call: 'gcd(12, 18)'\n    expect: '6'\n  - call: 'raises(lambda: 1 / 0)'\n    expect: 'ZeroDivisionError'\n---\ndef gcd(a, b):\n    return a if b == 0 else gcd(b, a % b)", "py-900.md");
        var result = await Runner.EvaluateAsync(snippet, snippet.Code, TimeSpan.FromSeconds(20));
        Assert.True(result.AllPassed, string.Join("; ", result.CompileErrors.Concat(result.Outcomes.Select(o => o.Actual))));
        var broken = await Runner.EvaluateAsync(snippet, "def gcd(a, b)\n    return 1", TimeSpan.FromSeconds(20));
        Assert.False(broken.Compiled);
    }

    [Fact]
    public async Task CHarnessPrintsThroughGenericDispatchAndReportsCrashes()
    {
        if (!Runner.Available(Language.C)) return;
        var snippet = SnippetLoader.Parse("---\nid: c-900\ntitle: T\nlang: c\ntests:\n  - call: 'half(9.0)'\n    expect: '4.5'\n  - call: 'is_pos(3)'\n    expect: 'true'\n  - call: 'name()'\n    expect: 'reps'\n---\ndouble half(double x) { return x / 2; }\nbool is_pos(int x) { return x > 0; }\nconst char *name(void) { return \"reps\"; }", "c-900.md");
        var result = await Runner.EvaluateAsync(snippet, snippet.Code, TimeSpan.FromSeconds(30));
        Assert.True(result.AllPassed, string.Join("; ", result.CompileErrors.Concat(result.Outcomes.Select(o => o.Actual))));
        var crash = await Runner.EvaluateAsync(snippet, "double half(double x) { int *p = 0; return *p; }\nbool is_pos(int x) { return x > 0; }\nconst char *name(void) { return \"reps\"; }", TimeSpan.FromSeconds(30));
        Assert.True(crash.Compiled);
        Assert.Contains("program stopped", crash.Outcomes[0].Actual);
    }

    [Fact]
    public async Task AssemblyLinksAgainstTheDeclaredPrototype()
    {
        if (!Runner.Available(Language.Asm) || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.Arm64) return;
        var snippet = SnippetLoader.Parse("---\nid: asm-900\ntitle: T\nlang: asm\ndecl: 'long twice(long);'\ntests:\n  - call: 'twice(21)'\n    expect: '42'\n---\n.text\n.globl _twice\n.p2align 2\n_twice:\n    lsl x0, x0, #1\n    ret", "asm-900.md");
        var result = await Runner.EvaluateAsync(snippet, snippet.Code, TimeSpan.FromSeconds(30));
        Assert.True(result.AllPassed, string.Join("; ", result.CompileErrors.Concat(result.Outcomes.Select(o => o.Actual))));
    }

    [Fact]
    public void LanguageIsInferredFromTheIdPrefixAndNormalisationIsLanguageAware()
    {
        var py = SnippetLoader.Parse("---\nid: py-001\ntitle: T\n---\ndef f():\n    return 1", "x.md");
        Assert.Equal(Language.Python, py.Language);
        Assert.NotEqual(TextDiff.Normalize("if x:\n    return 1\nreturn 2", Language.Python), TextDiff.Normalize("if x:\n    return 1\n    return 2", Language.Python));
        Assert.Equal(TextDiff.Normalize("add x0, x0, x1 // sum", Language.Asm), TextDiff.Normalize("add x0,x0,x1", Language.Asm));
    }
}

public class ProjectTests
{
    [Fact]
    public void ParsesStepsAndMarksThemDone()
    {
        var path = Path.Combine(Path.GetTempPath(), $"reps-project-{Guid.NewGuid():N}.md");
        File.WriteAllText(path, "---\nid: site\ntitle: A site\nstack: [react, firebase]\n---\nBuild the thing.\n\n## Steps\n- [ ] Scaffold\n- [x] Design\n- [ ] Deploy\n");
        try
        {
            var project = ProjectLoader.Parse(File.ReadAllText(path), path);
            Assert.Equal("site", project.Id);
            Assert.Equal(["react", "firebase"], project.Stack);
            Assert.Equal("Build the thing.", project.Goal);
            Assert.Equal(3, project.Steps.Count);
            Assert.Equal(1, project.DoneCount);
            Assert.Equal("Scaffold", project.NextStep!.Text);
            ProjectLoader.MarkDone(project, 1);
            var again = ProjectLoader.Parse(File.ReadAllText(path), path);
            Assert.Equal(2, again.DoneCount);
            Assert.Equal("Deploy", again.NextStep!.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WorkLogAccumulatesHours()
    {
        using var store = Store.InMemory();
        var today = new DateOnly(2026, 9, 26);
        store.LogWork("site", 1, today.ToDateTime(new TimeOnly(9, 0)), 1800, true);
        store.LogWork("site", 2, today.ToDateTime(new TimeOnly(10, 0)), 600, false);
        Assert.Equal(2400, store.WorkSeconds(today, today, "site"));
        Assert.Equal(1, store.StepsCompleted(today, today));
        Assert.Equal(1, store.Streak(today));
    }
}
