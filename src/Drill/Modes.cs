using System.Diagnostics;
using Spectre.Console;

namespace Drill;

public sealed record AttemptResult(Mode Mode, bool Passed, double Seconds, double? Accuracy, int Tries, bool Abandoned, string Note);

internal static class Ansi
{
    public const string Reset = "\e[0m";
    public const string Dim = "\e[2m";
    public const string Green = "\e[32m";
    public const string Red = "\e[31;4m";
    public const string ClearLine = "\e[K";

    /// A thin bar caret (DECSCUSR 6, plus iTerm2's own sequence), restored on the way out.
    public static string BarCursor => Environment.GetEnvironmentVariable("TERM_PROGRAM") == "iTerm.app"
        ? "\e[6 q\e]1337;CursorShape=1\a"
        : "\e[6 q";

    public static string DefaultCursor => Environment.GetEnvironmentVariable("TERM_PROGRAM") == "iTerm.app"
        ? "\e[0 q\e]1337;CursorShape=0\a"
        : "\e[0 q";
}

/// Trace: the reference sits on screen in dim grey and you type over it. Each key turns
/// its character green or red; Backspace takes it back; Enter steps to the next line and
/// the indentation is filled in for you; Tab is four spaces; Esc abandons (a fail).
/// Spacing is not graded: spaces between tokens fill in when the next character arrives
/// and stray spaces are ignored, so `a=0` and `a = 0` both pass.
public static class TraceMode
{
    public const double PassAccuracy = 0.97;

    public static AttemptResult Run(Snippet snippet)
    {
        var target = snippet.Code.Replace("\r\n", "\n").Replace("\t", "    ");
        var typed = new char?[target.Length];
        var auto = new bool[target.Length];
        var position = 0;
        var keystrokes = 0;
        var correct = 0;
        Stopwatch? clock = null;
        var abandoned = false;

        Console.Clear();
        Ui.Heading(snippet, "trace");
        AnsiConsole.MarkupLine("[grey]Type over the code. Enter ends a line, Tab indents, Backspace fixes, Esc gives up.[/]");
        AnsiConsole.WriteLine();
        var top = Ui.CurrentRow();
        var width = Ui.Width();
        var statusRow = top + BlockRows(target, width) + 1;
        Console.Write(Ansi.BarCursor);
        Console.CursorVisible = true;
        try
        {
            Render(target, typed, position, top, width);
            while (!(position == target.Length && AllCorrect(target, typed)))
            {
                // Poll instead of blocking so the clock under the code keeps moving.
                var lastTick = DateTime.MinValue;
                while (!Console.KeyAvailable)
                {
                    if ((DateTime.UtcNow - lastTick).TotalMilliseconds >= 50)
                    {
                        lastTick = DateTime.UtcNow;
                        DrawStatus(statusRow, clock, keystrokes, correct, position, target.Length);
                        PlaceCaret(target, position, top, width);
                    }
                    Thread.Sleep(15);
                }
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                {
                    abandoned = true;
                    break;
                }
                clock ??= Stopwatch.StartNew();

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (position > 0)
                    {
                        bool wasAuto;
                        do
                        {
                            position--;
                            typed[position] = null;
                            wasAuto = auto[position];
                            auto[position] = false;
                        } while (wasAuto && position > 0);
                    }
                    Render(target, typed, position, top, width);
                    continue;
                }
                if (position >= target.Length)
                {
                    continue; // at the end with errors left: only Backspace or Esc make sense
                }

                if (key.Key == ConsoleKey.Tab)
                {
                    var filled = 0;
                    while (filled < 4 && position < target.Length && target[position] == ' ')
                    {
                        typed[position] = ' ';
                        position++;
                        filled++;
                    }
                    keystrokes++;
                    if (filled > 0)
                    {
                        correct++;
                    }
                    else
                    {
                        typed[position] = '\t';
                        position++;
                    }
                    Render(target, typed, position, top, width);
                    continue;
                }

                var c = key.Key == ConsoleKey.Enter ? '\n' : key.KeyChar;
                if (c == '\0' || (char.IsControl(c) && c != '\n'))
                {
                    continue;
                }
                if (c == ' ' && target[position] != ' ')
                {
                    continue; // a space where the reference has none is style, not an error
                }
                if (c != ' ' && target[position] == ' ')
                {
                    // Spaces between tokens are filled in for you; the next real character
                    // is what gets graded.
                    while (position < target.Length && target[position] == ' ')
                    {
                        typed[position] = ' ';
                        auto[position] = true;
                        position++;
                    }
                    if (position >= target.Length)
                    {
                        Render(target, typed, position, top, width);
                        continue;
                    }
                }
                keystrokes++;
                typed[position] = c;
                if (c == target[position])
                {
                    correct++;
                }
                position++;
                if (c == '\n' && target[position - 1] == '\n')
                {
                    while (position < target.Length && target[position] == ' ')
                    {
                        typed[position] = ' ';
                        auto[position] = true;
                        position++;
                    }
                }
                Render(target, typed, position, top, width);
            }
        }
        finally
        {
            Console.Write(Ansi.Reset);
            Console.Write(Ansi.DefaultCursor);
            Console.CursorVisible = true;
            Console.Write($"\e[{statusRow + 1};1H{Ansi.ClearLine}\n");
        }

        var seconds = clock?.Elapsed.TotalSeconds ?? 0;
        var accuracy = keystrokes == 0 ? 0 : (double)correct / keystrokes;
        var passed = !abandoned && accuracy >= PassAccuracy;
        var wpm = seconds > 0 ? (int)Math.Round(target.Length / 5.0 / (seconds / 60.0)) : 0;
        var note = abandoned ? "abandoned" : $"{accuracy:P0} accuracy · {seconds:0.000}s · {wpm} wpm";
        return new AttemptResult(Mode.Trace, passed, seconds, accuracy, 1, abandoned, note);
    }

    private static bool AllCorrect(string target, char?[] typed)
    {
        for (var i = 0; i < target.Length; i++)
        {
            if (typed[i] != target[i])
            {
                return false;
            }
        }
        return true;
    }

    private static void Render(string target, char?[] typed, int position, int top, int width)
    {
        var output = new System.Text.StringBuilder();
        output.Append("\e[").Append(top + 1).Append(";1H");
        for (var i = 0; i < target.Length; i++)
        {
            var expected = target[i];
            string style;
            if (typed[i] is null || i == position)
            {
                style = Ansi.Dim;
            }
            else
            {
                style = typed[i] == expected ? Ansi.Green : Ansi.Red;
            }
            if (expected == '\n')
            {
                output.Append(style).Append('↵').Append(Ansi.Reset).Append(Ansi.ClearLine).Append('\n');
            }
            else
            {
                var wrong = typed[i] is { } t && t != expected && i != position;
                var shown = wrong && typed[i] is { } w && !char.IsControl(w) ? w : expected;
                if (wrong && shown == ' ')
                {
                    shown = '·';
                }
                output.Append(style).Append(shown).Append(Ansi.Reset);
            }
        }
        output.Append(Ansi.ClearLine);
        Console.Write(output.ToString());
        PlaceCaret(target, position, top, width);
    }

    /// Puts the terminal cursor on the cell of the next expected character, so the caret
    /// the user sees is the terminal's own thin bar rather than a painted block.
    private static void PlaceCaret(string target, int position, int top, int width)
    {
        var row = top;
        var col = 0;
        for (var i = 0; i < position && i < target.Length; i++)
        {
            if (target[i] == '\n')
            {
                row++;
                col = 0;
                continue;
            }
            col++;
            if (col >= width)
            {
                row++;
                col = 0;
            }
        }
        Console.Write($"\e[{row + 1};{col + 1}H");
    }

    /// Rows the code block occupies on screen, counting the end-of-line glyph and wraps.
    private static int BlockRows(string target, int width)
    {
        var rows = 0;
        foreach (var line in target.Split('\n'))
        {
            rows += Math.Max(1, (line.Length + 1 + width - 1) / width);
        }
        return rows;
    }

    private static void DrawStatus(int statusRow, Stopwatch? clock, int keystrokes, int correct, int position, int total)
    {
        var elapsed = clock?.Elapsed ?? TimeSpan.Zero;
        string text;
        if (clock is null)
        {
            text = "00:00.000 · start typing";
        }
        else
        {
            var accuracy = keystrokes == 0 ? 1.0 : (double)correct / keystrokes;
            var wpm = elapsed.TotalSeconds > 0 ? (int)Math.Round(position / 5.0 / (elapsed.TotalSeconds / 60.0)) : 0;
            var colour = accuracy >= PassAccuracy ? Ansi.Green : "\e[33m";
            text = $"{elapsed:mm\\:ss\\.fff} · {colour}{accuracy:P0}{Ansi.Reset}{Ansi.Dim} · {wpm} wpm · {position}/{total}";
        }
        Console.Write($"\e[{statusRow + 1};1H{Ansi.Dim}{text}{Ansi.Reset}{Ansi.ClearLine}");
    }
}

/// Recall: read the reference for as long as you need, hide it, then write it yourself.
/// Where the snippet has tests, the tests decide, so a different but working version
/// passes; without tests the comparison is exact once formatting is ignored. After a miss,
/// r shows the reference again for another go; only the first go counts as a first try.
public static class RecallMode
{
    public static async Task<AttemptResult> RunAsync(Snippet snippet)
    {
        var byTests = snippet.Tests.Count > 0 && Runner.Available(snippet.Language);
        var text = "";
        var tries = 0;
        var reading = TimeSpan.Zero;
        var clock = new Stopwatch();
        while (true)
        {
            var looked = Show(snippet, tries);
            if (looked is null)
            {
                return new AttemptResult(Mode.Recall, false, clock.Elapsed.TotalSeconds, null, Math.Max(1, tries), true, "abandoned");
            }
            reading += looked.Value;

            var header = new List<string> { (byTests ? "Write it your way; the tests decide." : "Write it back; no tests for this one, so it has to match.") + (tries == 0 ? "" : $" · try {tries + 1}") };
            if (snippet.Spec.Length > 0)
            {
                header.Add(snippet.Spec);
            }
            if (byTests)
            {
                header.Add("tests:");
                header.AddRange(snippet.Tests.Select(t => $"  {t.Call}  =>  {t.Expect}"));
            }
            clock.Start();
            var (edited, _) = Ui.Compose(snippet, "recall", header, text);
            clock.Stop();
            if (edited is null)
            {
                return new AttemptResult(Mode.Recall, false, clock.Elapsed.TotalSeconds, null, Math.Max(1, tries), true, "abandoned");
            }
            text = edited;
            if (text.Trim().Length == 0)
            {
                return new AttemptResult(Mode.Recall, false, clock.Elapsed.TotalSeconds, null, Math.Max(1, tries), true, "nothing written");
            }
            tries++;

            Console.Clear();
            Ui.Heading(snippet, "recall");
            bool passed;
            string verdict;
            if (byTests)
            {
                var result = await AnsiConsole.Status().StartAsync(
                    "compiling and running the tests…",
                    _ => Runner.EvaluateAsync(snippet, text, TimeSpan.FromSeconds(10)));
                BlankMode.ShowResult(result);
                passed = result.AllPassed;
                verdict = passed ? "all tests" : "tests failed";
            }
            else
            {
                var distance = TextDiff.Levenshtein(TextDiff.Normalize(text, snippet.Language), TextDiff.Normalize(snippet.Code, snippet.Language));
                passed = distance == 0;
                verdict = passed ? "exact" : $"{distance} edits away";
            }
            var timing = $"read {reading.TotalSeconds:0}s · wrote {clock.Elapsed.TotalSeconds:0.000}s";
            if (passed)
            {
                return new AttemptResult(Mode.Recall, true, clock.Elapsed.TotalSeconds, null, tries, false,
                    $"{verdict} · {(tries == 1 ? "first try" : $"try {tries}")} · {timing}");
            }
            if (byTests)
            {
                AnsiConsole.Write(new Panel(new Text(snippet.Code)).Border(BoxBorder.Rounded).Header("[grey] reference [/]"));
            }
            else
            {
                ShowDiff(snippet, text);
            }
            AnsiConsole.MarkupLine("[grey]r to read it again and have another go · Enter to move on[/]");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is not ('r' or 'R'))
            {
                return new AttemptResult(Mode.Recall, false, clock.Elapsed.TotalSeconds, null, tries, false,
                    $"{verdict} · {(tries == 1 ? "1 try" : $"{tries} tries")} · {timing}");
            }
        }
    }

    /// Shows the reference until Enter and returns how long it was on screen; null on Esc.
    private static TimeSpan? Show(Snippet snippet, int tries)
    {
        Console.Clear();
        Ui.Heading(snippet, "recall");
        AnsiConsole.Write(new Panel(new Text(snippet.Code)).Border(BoxBorder.Rounded).Header(tries == 0 ? "[grey] read it [/]" : "[grey] read it again [/]"));
        AnsiConsole.MarkupLine("[grey]Take the time you need; hold the idea, not the text. Enter hides it and you write it. Esc gives up.[/]");
        var clock = Stopwatch.StartNew();
        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                Console.Write($"\r{Ansi.Dim}reading {clock.Elapsed:m\\:ss}{Ansi.Reset}{Ansi.ClearLine}");
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        return null;
                    }
                    if (key.Key == ConsoleKey.Enter)
                    {
                        return clock.Elapsed;
                    }
                }
                Thread.Sleep(100);
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    private static void ShowDiff(Snippet snippet, string text)
    {
        var table = new Table().Border(TableBorder.Rounded).AddColumn("reference").AddColumn("yours");
        foreach (var (expected, actual, same) in TextDiff.LineReport(snippet.Code, text, snippet.Language))
        {
            var colour = same ? "green" : "red";
            table.AddRow($"[{colour}]{Markup.Escape(expected)}[/]", $"[{colour}]{Markup.Escape(actual)}[/]");
        }
        AnsiConsole.Write(table);
    }
}

/// Blank: a one-line spec and the tests, nothing else. Write the code, Ctrl+D runs the
/// tests. Fixing and rerunning is allowed, but only a first-try pass counts toward the
/// weekly number.
public static class BlankMode
{
    public static async Task<AttemptResult> RunAsync(Snippet snippet, TimeSpan? timeLimit = null, string? hint = null)
    {
        var deadline = timeLimit is null ? (DateTime?)null : DateTime.UtcNow + timeLimit.Value;
        var header = new List<string> { snippet.Spec };
        if (hint is not null)
        {
            header.Add(hint);
        }
        if (timeLimit is not null)
        {
            header.Add($"interview mode: {timeLimit.Value.TotalMinutes:0} minutes, no hints, one submission");
        }
        if (snippet.Decl.Length > 0)
        {
            header.Add($"signature: {snippet.Decl}");
        }
        header.Add("tests:");
        header.AddRange(snippet.Tests.Select(t => $"  {t.Call}  =>  {t.Expect}"));
        var text = "";
        var tries = 0;
        var clock = Stopwatch.StartNew();
        while (true)
        {
            var (edited, timedOut) = Ui.Compose(snippet, timeLimit is null ? "blank" : "interview", header, text, deadline);
            if (edited is null)
            {
                clock.Stop();
                return new AttemptResult(Mode.Blank, false, clock.Elapsed.TotalSeconds, null, Math.Max(1, tries), true, "abandoned");
            }
            text = edited;
            if (timedOut)
            {
                clock.Stop();
                var late = await Runner.EvaluateAsync(snippet, text, TimeSpan.FromSeconds(10));
                Console.Clear();
                Ui.Heading(snippet, "interview");
                ShowResult(late);
                return new AttemptResult(Mode.Blank, false, clock.Elapsed.TotalSeconds, null, Math.Max(1, tries + 1), false,
                    late.AllPassed ? "correct, but over time" : "time is up");
            }
            if (text.Trim().Length == 0)
            {
                clock.Stop();
                return new AttemptResult(Mode.Blank, false, clock.Elapsed.TotalSeconds, null, Math.Max(1, tries), true, "nothing written");
            }
            tries++;

            Console.Clear();
            Ui.Heading(snippet, "blank");
            var result = await AnsiConsole.Status().StartAsync(
                "compiling and running the tests…",
                _ => Runner.EvaluateAsync(snippet, text, TimeSpan.FromSeconds(10)));
            ShowResult(result);
            if (result.AllPassed)
            {
                clock.Stop();
                var note = tries == 1 ? $"all tests · first try · {clock.Elapsed.TotalSeconds:0.000}s" : $"all tests · try {tries} · {clock.Elapsed.TotalSeconds:0.000}s";
                return new AttemptResult(Mode.Blank, true, clock.Elapsed.TotalSeconds, null, tries, false, note);
            }
            if (timeLimit is not null)
            {
                clock.Stop();
                return new AttemptResult(Mode.Blank, false, clock.Elapsed.TotalSeconds, null, tries, false, "interview: one submission, tests failed");
            }
            AnsiConsole.MarkupLine("[grey]Enter to keep editing · q to stop here[/]");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q' || key.Key == ConsoleKey.Escape)
            {
                clock.Stop();
                return new AttemptResult(Mode.Blank, false, clock.Elapsed.TotalSeconds, null, tries, false, $"failed after {tries} {(tries == 1 ? "try" : "tries")}");
            }
        }
    }

    internal static void ShowResult(EvalResult result)
    {
        if (!result.Compiled)
        {
            AnsiConsole.MarkupLine("[red]did not compile[/]");
            foreach (var error in result.CompileErrors)
            {
                AnsiConsole.MarkupLine($"  [red]{Markup.Escape(error)}[/]");
            }
            return;
        }
        var table = new Table().Border(TableBorder.Rounded).AddColumn("").AddColumn("call").AddColumn("expected").AddColumn("got");
        foreach (var outcome in result.Outcomes)
        {
            var mark = outcome.Passed ? "[green]pass[/]" : "[red]fail[/]";
            table.AddRow(mark, Markup.Escape(outcome.Test.Call), Markup.Escape(outcome.Test.Expect), Markup.Escape(outcome.Actual));
        }
        AnsiConsole.Write(table);
    }
}

/// Predict: the code stays on screen and each test call is asked in turn; you type what
/// it returns before anything runs. This trains the model in your head, not your fingers,
/// and it is where integer promotion, pointer arithmetic and off-by-ones get learned.
public static class PredictMode
{
    public static AttemptResult Run(Snippet snippet)
    {
        Console.Clear();
        Ui.Heading(snippet, "predict");
        AnsiConsole.Write(new Panel(new Text(snippet.Code)).Border(BoxBorder.Rounded).Header("[grey] read it [/]"));
        if (snippet.Decl.Length > 0)
        {
            AnsiConsole.MarkupLine($"[grey]signature: {Markup.Escape(snippet.Decl)}[/]");
        }
        AnsiConsole.MarkupLine("[grey]For each call, type what it returns and press Enter. An empty line gives up.[/]");
        var clock = Stopwatch.StartNew();
        var right = 0;
        var asked = 0;
        foreach (var test in snippet.Tests)
        {
            AnsiConsole.Markup($"  [cyan]{Markup.Escape(test.Call)}[/] → ");
            var given = Console.ReadLine();
            if (given is null || given.Trim().Length == 0)
            {
                clock.Stop();
                return new AttemptResult(Mode.Predict, false, clock.Elapsed.TotalSeconds, asked == 0 ? null : (double)right / asked, 1, true, "abandoned");
            }
            asked++;
            var ok = Matches(given, test.Expect);
            if (ok) right++;
            AnsiConsole.MarkupLine(ok ? "    [green]✓[/]" : $"    [red]✗[/] [grey]it returns[/] {Markup.Escape(test.Expect)}");
        }
        clock.Stop();
        var passed = right == asked;
        return new AttemptResult(Mode.Predict, passed, clock.Elapsed.TotalSeconds, (double)right / asked, 1, false,
            $"{right}/{asked} predicted · {clock.Elapsed.TotalSeconds:0.000}s");
    }

    /// Exact after trimming; otherwise equal with quotes and spaces ignored, booleans and
    /// None/null case-insensitively, or as numbers.
    public static bool Matches(string given, string expected)
    {
        var g = given.Trim();
        var e = expected.Trim();
        if (g == e)
        {
            return true;
        }
        if (g.Length >= 2 && ((g[0] == '"' && g[^1] == '"') || (g[0] == '\'' && g[^1] == '\'')))
        {
            g = g[1..^1];
        }
        if (g == e || g.Replace(" ", "") == e.Replace(" ", ""))
        {
            return true;
        }
        if (string.Equals(g, e, StringComparison.OrdinalIgnoreCase) && e.ToLowerInvariant() is "true" or "false" or "none" or "null")
        {
            return true;
        }
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var styles = System.Globalization.NumberStyles.Float;
        if (double.TryParse(g, styles, culture, out var gd) && double.TryParse(e, styles, culture, out var ed))
        {
            return Math.Abs(gd - ed) < 1e-9;
        }
        return false;
    }
}

internal static class Ui
{
    public static void Heading(Snippet snippet, string mode)
    {
        var tags = snippet.Tags.Count > 0 ? $"  [grey]{Markup.Escape(string.Join(", ", snippet.Tags))}[/]" : "";
        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(snippet.Id)}[/] [bold]{Markup.Escape(snippet.Title)}[/]  [grey]{snippet.Language.DisplayName()}[/]{tags}  [grey]·[/] [yellow]{mode}[/]");
    }

    /// Opens the built-in editor (or DRILL_EDITOR when set) under a plain-text header.
    public static (string? Text, bool TimedOut) Compose(Snippet snippet, string mode, IReadOnlyList<string> lines, string initial, DateTime? deadline = null)
    {
        if (ExternalEditor.Command is not null)
        {
            var prefix = snippet.Language == Language.Python ? "# " : "// ";
            var comment = string.Join("\n", lines.Select(l => prefix + l)) + "\n\n";
            var edited = ExternalEditor.Edit(comment + initial, snippet.Language.Extension(), lines.Count + 2);
            if (edited is null)
            {
                return (null, false);
            }
            edited = edited.Replace("\r\n", "\n");
            var body = edited.StartsWith(comment, StringComparison.Ordinal) ? edited[comment.Length..] : edited;
            return (body, deadline is not null && DateTime.UtcNow > deadline.Value);
        }
        var header = new List<string> { $"{snippet.Id} {snippet.Title} · {snippet.Language.DisplayName()} · {mode}" };
        header.AddRange(lines);
        var box = new TextBox(header, initial, snippet.Language);
        var text = box.Run(deadline);
        return (text, box.TimedOut);
    }

    public static int Width()
    {
        try
        {
            return Math.Max(40, Console.WindowWidth);
        }
        catch
        {
            return 120;
        }
    }

    /// The terminal's current row, without trusting Console.CursorTop on odd terminals.
    public static int CurrentRow()
    {
        try
        {
            return Console.CursorTop;
        }
        catch
        {
            return 3;
        }
    }
}
