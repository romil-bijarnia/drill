using Reps;
using Xunit;

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
        foreach (var snippet in snippets.Where(s => s.Tests.Count > 0))
        {
            var result = await Evaluator.RunAsync(snippet.Code, snippet.Tests, TimeSpan.FromSeconds(20));
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
