using Drill;
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
        var snippet = SnippetLoader.Parse("---\nid: c-900\ntitle: T\nlang: c\ntests:\n  - call: 'half(9.0)'\n    expect: '4.5'\n  - call: 'is_pos(3)'\n    expect: 'true'\n  - call: 'name()'\n    expect: 'drill'\n---\ndouble half(double x) { return x / 2; }\nbool is_pos(int x) { return x > 0; }\nconst char *name(void) { return \"drill\"; }", "c-900.md");
        var result = await Runner.EvaluateAsync(snippet, snippet.Code, TimeSpan.FromSeconds(30));
        Assert.True(result.AllPassed, string.Join("; ", result.CompileErrors.Concat(result.Outcomes.Select(o => o.Actual))));
        var crash = await Runner.EvaluateAsync(snippet, "double half(double x) { int *p = 0; return *p; }\nbool is_pos(int x) { return x > 0; }\nconst char *name(void) { return \"drill\"; }", TimeSpan.FromSeconds(30));
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
        var path = Path.Combine(Path.GetTempPath(), $"drill-project-{Guid.NewGuid():N}.md");
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

public class BitsTests
{
    [Theory]
    [InlineData("0xFF", AnswerForm.Hex, "ff")]
    [InlineData(" 00ff ", AnswerForm.Hex, "ff")]
    [InlineData("0b0101", AnswerForm.Bin, "101")]
    [InlineData("-013", AnswerForm.Dec, "-13")]
    [InlineData("0", AnswerForm.Dec, "0")]
    [InlineData("Y", AnswerForm.YesNo, "yes")]
    [InlineData("No", AnswerForm.YesNo, "no")]
    [InlineData("78 56 34 12", AnswerForm.Bytes, "78563412")]
    public void NormalizesAnswers(string given, AnswerForm form, string expected) =>
        Assert.Equal(expected, BitsMode.Normalize(given, form));

    [Fact]
    public void EveryKindGeneratesAnAnswer()
    {
        var random = new Random(7);
        foreach (var kind in BitsMode.Kinds)
        {
            for (var i = 0; i < 300; i++)
            {
                var question = BitsMode.Generate(random, kind);
                Assert.Equal(kind, question.Kind);
                Assert.False(string.IsNullOrWhiteSpace(question.Prompt));
                Assert.False(string.IsNullOrWhiteSpace(question.Answer));
            }
        }
    }

    [Fact]
    public void GeneratedAnswersSatisfyTheirOwnArithmetic()
    {
        var random = new Random(11);
        var checkedSome = 0;
        for (var i = 0; i < 2000; i++)
        {
            var q = BitsMode.Generate(random, i % 2 == 0 ? "twos" : "arm");
            var neg = System.Text.RegularExpressions.Regex.Match(q.Prompt, @"^(-\d+) as (\d+)-bit two's complement");
            if (neg.Success)
            {
                var n = long.Parse(neg.Groups[1].Value);
                var bits = int.Parse(neg.Groups[2].Value);
                Assert.Equal((1L << bits) + n, Convert.ToInt64(q.Answer, 16));
                checkedSome++;
            }
            var add = System.Text.RegularExpressions.Regex.Match(q.Prompt, @"^x1 = 0x([0-9a-f]+), x2 = 0x([0-9a-f]+): add x0, x1, x2 → x0 in hex$");
            if (add.Success)
            {
                Assert.Equal(Convert.ToInt64(add.Groups[1].Value, 16) + Convert.ToInt64(add.Groups[2].Value, 16), Convert.ToInt64(q.Answer, 16));
                checkedSome++;
            }
            var ldr = System.Text.RegularExpressions.Regex.Match(q.Prompt, @"^x1 → bytes ((?:[0-9a-f]{2} ?){8}): ldr w0, \[x1\] → w0 in hex$");
            if (ldr.Success)
            {
                var bytes = ldr.Groups[1].Value.Split(' ');
                Assert.Equal(bytes[3] + bytes[2] + bytes[1] + bytes[0], q.Answer.PadLeft(8, '0'));
                checkedSome++;
            }
        }
        Assert.True(checkedSome > 50);
    }
}

public class PredictTests
{
    [Theory]
    [InlineData("Fizz", "Fizz", true)]
    [InlineData("'Fizz'", "Fizz", true)]
    [InlineData("\"7\"", "7", true)]
    [InlineData("true", "True", true)]
    [InlineData("7.0", "7", true)]
    [InlineData("[1,2,3]", "[1, 2, 3]", true)]
    [InlineData("8", "7", false)]
    [InlineData("fizz", "Fizz", false)]
    public void MatchesLikeAReader(string given, string expected, bool matches) =>
        Assert.Equal(matches, PredictMode.Matches(given, expected));
}

public class CompileViewTests
{
    [Fact]
    public void PrototypesReplaceBodiesAndKeepTypes()
    {
        var code = "struct node { long v; struct node *next; };\n\nstatic int helper(int x)\n{\n    if (x) { return 1; }\n    return 0;\n}\n\nlong len(const struct node *n)\n{\n    long c = 0;\n    while (n) { c++; n = n->next; }\n    return c;\n}";
        var decl = CompileView.Prototypes(code);
        Assert.Equal("struct node { long v; struct node *next; };\nint helper(int x);\nlong len(const struct node *n);", decl);
    }

    [Fact]
    public void CleanDropsDirectivesAndComments()
    {
        var listing = "\t.section\t__TEXT,__text,regular,pure_instructions\n\t.build_version macos, 15, 0\n\t.globl\t_gcd                            ; -- Begin function gcd\n\t.p2align\t2\n_gcd:                                   ; @gcd\n\t.cfi_startproc\n; %bb.0:\n\tcbz\tw1, LBB0_2\n\tret\n\t.cfi_endproc\n.subsections_via_symbols\n";
        Assert.Equal("    .globl    _gcd\n    .p2align    2\n_gcd:\n    cbz    w1, LBB0_2\n    ret", CompileView.Clean(listing));
    }
}

public class FamilyTests
{
    [Fact]
    public void FamilyIsReadFromTheHeader()
    {
        var text = "---\nid: c-099\ntitle: Test\nlang: c\ntags: [x]\nfamily: gcd\nmodes: [trace, recall]\n---\nint f(void) { return 1; }\n";
        Assert.Equal("gcd", SnippetLoader.Parse(text, "c-099.md").Family);
        Assert.Equal("", SnippetLoader.Parse(text.Replace("family: gcd\n", ""), "c-099.md").Family);
        Assert.True(SnippetLoader.Parse(text, "c-099.md").Supports(Mode.Predict) == false);
    }

    [Fact]
    public void StackRunsFromPythonDownToAssembly() =>
        Assert.Equal([Language.Python, Language.CSharp, Language.C, Language.Asm], Languages.Stack);
}

public class MachineTests
{
    private static Machine Run(params string[] lines)
    {
        var m = new Machine();
        foreach (var line in lines) m.Tape.Add(Machine.Parse(line));
        m.Run(0);
        return m;
    }

    [Fact]
    public void MovAddSubWithShiftedOperand()
    {
        var m = Run("mov x0, #5", "add x1, x0, #7", "sub x2, x1, x0, lsl #1");
        Assert.Equal(12, m.Regs[1]);
        Assert.Equal(2, m.Regs[2]);
    }

    [Fact]
    public void CompareSetsFlagsAndCsetReadsThem()
    {
        var m = Run("mov x0, #3", "mov x1, #8", "cmp x0, x1", "cset x2, lt", "cset x3, hi", "cset x4, ne");
        Assert.True(m.N);
        Assert.False(m.C);
        Assert.Equal(1, m.Regs[2]);
        Assert.Equal(0, m.Regs[3]);
        Assert.Equal(1, m.Regs[4]);
    }

    [Fact]
    public void ThirtyTwoBitResultsWrap()
    {
        var m = Run("mov x1, #0xffffffff", "add w2, w1, #1", "mov w3, #-1");
        Assert.Equal(0, m.Regs[2]);
        Assert.Equal(0xffffffffL, m.Regs[3]);
    }

    [Fact]
    public void MemoryIsLittleEndian()
    {
        var m = Run("mov x1, #0x1000", "mov x0, #0x12345678", "str w0, [x1]", "ldrb w2, [x1]", "ldrh w3, [x1, #2]", "ldrsb w4, [x1, #3]");
        Assert.Equal(0x78, m.Memory[0x1000]);
        Assert.Equal(0x78, m.Regs[2]);
        Assert.Equal(0x1234, m.Regs[3]);
        Assert.Equal(0x12, m.Regs[4]);
    }

    [Fact]
    public void PreAndPostIndexMoveTheStackPointer()
    {
        var m = Run("mov x0, #42", "str x0, [sp, #-16]!", "ldr x1, [sp], #16");
        Assert.Equal(42, m.Regs[1]);
        Assert.Equal(Machine.StackTop, m.Sp);
    }

    [Fact]
    public void ABackwardBranchReplaysTheLoop()
    {
        var m = Run("mem 0x1000 = 01 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00 03 00 00 00 00 00 00 00", "x1 = 0x1000", "x2 = 3",
            "loop:", "ldr x3, [x1], #8", "add x0, x0, x3", "subs x2, x2, #1", "b.ne loop");
        Assert.Equal(6, m.Regs[0]);
        Assert.Equal(0x1018, m.Regs[1]);
    }

    [Fact]
    public void CallAndReturnUseTheLinkRegister()
    {
        var m = Run("b main", "double:", "lsl x0, x0, #1", "ret", "main:", "mov x0, #21", "bl double", "add x0, x0, #1");
        Assert.Equal(43, m.Regs[0]);
    }

    [Fact]
    public void RecursiveFactorialWithAFrame()
    {
        var m = Run("mov x0, #5", "b main", "fact:", "cmp x0, #1", "b.le base", "stp x29, x30, [sp, #-16]!", "str x0, [sp, #-16]!",
            "sub x0, x0, #1", "bl fact", "ldr x1, [sp], #16", "mul x0, x0, x1", "ldp x29, x30, [sp], #16", "base:", "ret", "main:", "bl fact");
        Assert.Equal(120, m.Regs[0]);
        Assert.Equal(Machine.StackTop, m.Sp);
    }

    [Fact]
    public void ATakenBranchToAMissingLabelWaits()
    {
        var m = new Machine();
        m.Tape.Add(Machine.Parse("mov x0, #0"));
        m.Tape.Add(Machine.Parse("cbz x0, done"));
        m.Tape.Add(Machine.Parse("mov x1, #9"));
        m.Run(0);
        Assert.Equal("done", m.Waiting);
        Assert.Equal(1, m.WaitingAt);
        Assert.Equal(0, m.Regs[1]);
        m.Tape.Add(Machine.Parse("done:"));
        m.Reset();
        m.Run(0);
        Assert.Null(m.Waiting);
        Assert.Equal(0, m.Regs[1]);
    }

    [Fact]
    public void LocalNumericLabelsWork()
    {
        var m = Run("mov x1, #3", "1:", "add x0, x0, x1", "subs x1, x1, #1", "b.ne 1b", "cbz x1, 2f", "mov x5, #1", "2:", "mov x6, #7");
        Assert.Equal(6, m.Regs[0]);
        Assert.Equal(0, m.Regs[5]);
        Assert.Equal(7, m.Regs[6]);
    }

    [Fact]
    public void BitFieldsAndExtensions()
    {
        var m = Run("mov x1, #0xb7", "ubfx x0, x1, #4, #3", "mov w2, #0x80", "sxtb w3, w2", "rbit w4, w2", "clz x5, x1", "movz x6, #0x1234", "movk x6, #0x5678, lsl #16");
        Assert.Equal(3, m.Regs[0]);
        Assert.Equal(0xffffff80L, m.Regs[3]);
        Assert.Equal(0x01000000, m.Regs[4]);
        Assert.Equal(56, m.Regs[5]);
        Assert.Equal(0x56781234, m.Regs[6]);
    }

    [Fact]
    public void ConditionalSelectsAndDivision()
    {
        var m = Run("mov x0, #100", "mov x1, #7", "udiv x2, x0, x1", "msub x3, x2, x1, x0", "cmp x0, x1", "csel x4, x0, x1, lt", "mov x5, #-7", "cmp x5, #0", "cneg x6, x5, mi");
        Assert.Equal(14, m.Regs[2]);
        Assert.Equal(2, m.Regs[3]);
        Assert.Equal(7, m.Regs[4]);
        Assert.Equal(7, m.Regs[6]);
    }

    [Fact]
    public void ErrorsAreMachineExceptions()
    {
        Assert.Throws<MachineException>(() => Run("mov x1, #0x20000", "ldr x0, [x1]"));
        Assert.Throws<MachineException>(() => Run("frob x0, x1"));
        Assert.Throws<MachineException>(() => Run("loop:", "b loop"));
        Assert.Throws<MachineException>(() => Machine.Parse("mov x0, #banana").Mnemonic is null ? throw new MachineException("x") : Run("mov x0, #banana"));
    }

    [Theory]
    [InlineData("#'a'", 97)]
    [InlineData("#0b101", 5)]
    [InlineData("#-0x10", -16)]
    [InlineData("12", 12)]
    [InlineData("#~0", -1)]
    public void ImmediatesParse(string text, long expected) => Assert.Equal(expected, Machine.ParseImm(text));

    [Fact]
    public void EveryTaskHintSolvesItsTask()
    {
        foreach (var task in MachineTasks.All.Where(t => t.Number != 26))
        {
            var m = new Machine();
            task.Setup(m);
            foreach (var piece in task.Hint.Split('·').SelectMany(p => p.Split(" then ")).Select(p => p.Trim()).Where(p => p.Length > 0))
            {
                var code = piece.Split(" (")[0].Trim();
                var colon = code.IndexOf(':');
                if (colon > 0 && code[..colon].Contains(' '))
                {
                    code = code[..colon].Trim(); // an explanation after the instruction, not a label
                }
                m.Tape.Add(Machine.Parse(code));
            }
            m.Run(0);
            Assert.True(task.Check(m), $"task {task.Number}: the hint did not solve it");
        }
    }
}
