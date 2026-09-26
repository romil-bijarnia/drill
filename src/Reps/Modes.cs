using System.Diagnostics;
using Spectre.Console;

namespace Reps;

public sealed record AttemptResult(Mode Mode, bool Passed, double Seconds, double? Accuracy, int Tries, bool Abandoned, string Note);

internal static class Ansi
{
    public const string Reset = "\e[0m";
    public const string Dim = "\e[2m";
    public const string Green = "\e[32m";
    public const string Red = "\e[31;4m";
    public const string Cursor = "\e[7m";
    public const string ClearLine = "\e[K";
}

/// Trace: the reference sits on screen in dim grey and you type over it. Each key turns
/// its character green or red; Backspace takes it back; Enter steps to the next line and
/// the indentation is filled in for you; Tab is four spaces; Esc abandons (a fail).
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
        Console.CursorVisible = false;
        try
        {
            Render(target, typed, position, top);
            while (!(position == target.Length && AllCorrect(target, typed)))
            {
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
                    Render(target, typed, position, top);
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
                    Render(target, typed, position, top);
                    continue;
                }

                var c = key.Key == ConsoleKey.Enter ? '\n' : key.KeyChar;
                if (c == '\0' || (char.IsControl(c) && c != '\n'))
                {
                    continue;
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
                Render(target, typed, position, top);
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.Write(Ansi.Reset);
            Console.Write("\n\n");
        }

        var seconds = clock?.Elapsed.TotalSeconds ?? 0;
        var accuracy = keystrokes == 0 ? 0 : (double)correct / keystrokes;
        var passed = !abandoned && accuracy >= PassAccuracy;
        var wpm = seconds > 0 ? (int)Math.Round(target.Length / 5.0 / (seconds / 60.0)) : 0;
        var note = abandoned ? "abandoned" : $"{accuracy:P0} accuracy · {seconds:0}s · {wpm} wpm";
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

    private static void Render(string target, char?[] typed, int position, int top)
    {
        var output = new System.Text.StringBuilder();
        output.Append("\e[").Append(top + 1).Append(";1H");
        for (var i = 0; i < target.Length; i++)
        {
            var expected = target[i];
            string style;
            if (i == position)
            {
                style = Ansi.Cursor;
            }
            else if (typed[i] is null)
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
    }
}

/// Recall: the snippet shows for a few seconds, then disappears; you type it from memory
/// and the trainer compares the two with formatting ignored.
public static class RecallMode
{
    public static AttemptResult Run(Snippet snippet)
    {
        Console.Clear();
        Ui.Heading(snippet, "recall");
        var seconds = Math.Clamp(snippet.Code.Length / 10, 8, 30);
        AnsiConsole.Write(new Panel(new Text(snippet.Code)).Border(BoxBorder.Rounded).Header("[grey] memorise [/]"));
        AnsiConsole.MarkupLine("[grey]Enter hides it early. Esc gives up.[/]");
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        var abandoned = false;
        Console.CursorVisible = false;
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                var remaining = (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds);
                Console.Write($"\r{Ansi.Dim}hidden in {remaining,2}s{Ansi.Reset}{Ansi.ClearLine}");
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        abandoned = true;
                        break;
                    }
                    if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
                Thread.Sleep(100);
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
        if (abandoned)
        {
            return new AttemptResult(Mode.Recall, false, 0, null, 1, true, "abandoned");
        }

        var clock = Stopwatch.StartNew();
        var text = Ui.Compose(snippet, "recall", ["Type it from memory."], "");
        clock.Stop();
        if (text is null)
        {
            return new AttemptResult(Mode.Recall, false, clock.Elapsed.TotalSeconds, null, 1, true, "abandoned");
        }

        var distance = TextDiff.Levenshtein(TextDiff.Normalize(text), TextDiff.Normalize(snippet.Code));
        var passed = distance == 0 && text.Trim().Length > 0;
        Console.Clear();
        Ui.Heading(snippet, "recall");
        if (!passed)
        {
            var table = new Table().Border(TableBorder.Rounded).AddColumn("reference").AddColumn("yours");
            foreach (var (expected, actual, same) in TextDiff.LineReport(snippet.Code, text))
            {
                var colour = same ? "green" : "red";
                table.AddRow($"[{colour}]{Markup.Escape(expected)}[/]", $"[{colour}]{Markup.Escape(actual)}[/]");
            }
            AnsiConsole.Write(table);
        }
        var note = passed ? $"exact · {clock.Elapsed.TotalSeconds:0}s" : $"{distance} edits away · {clock.Elapsed.TotalSeconds:0}s";
        return new AttemptResult(Mode.Recall, passed, clock.Elapsed.TotalSeconds, null, 1, false, note);
    }
}

/// Blank: a one-line spec and the tests, nothing else. Write the code, Ctrl+D runs the
/// tests. Fixing and rerunning is allowed, but only a first-try pass counts toward the
/// weekly number.
public static class BlankMode
{
    public static async Task<AttemptResult> RunAsync(Snippet snippet)
    {
        var header = new List<string> { snippet.Spec, "tests:" };
        header.AddRange(snippet.Tests.Select(t => $"  {t.Call}  =>  {t.Expect}"));
        var text = "";
        var tries = 0;
        var clock = Stopwatch.StartNew();
        while (true)
        {
            var edited = Ui.Compose(snippet, "blank", header, text);
            if (edited is null)
            {
                clock.Stop();
                return new AttemptResult(Mode.Blank, false, clock.Elapsed.TotalSeconds, null, Math.Max(1, tries), true, "abandoned");
            }
            text = edited;
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
                _ => Evaluator.RunAsync(text, snippet.Tests, TimeSpan.FromSeconds(10)));
            ShowResult(result);
            if (result.AllPassed)
            {
                clock.Stop();
                var note = tries == 1 ? $"all tests · first try · {clock.Elapsed.TotalSeconds:0}s" : $"all tests · try {tries} · {clock.Elapsed.TotalSeconds:0}s";
                return new AttemptResult(Mode.Blank, true, clock.Elapsed.TotalSeconds, null, tries, false, note);
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

    private static void ShowResult(EvalResult result)
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

internal static class Ui
{
    public static void Heading(Snippet snippet, string mode)
    {
        var tags = snippet.Tags.Count > 0 ? $"  [grey]{Markup.Escape(string.Join(", ", snippet.Tags))}[/]" : "";
        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(snippet.Id)}[/] [bold]{Markup.Escape(snippet.Title)}[/]{tags}  [grey]·[/] [yellow]{mode}[/]");
    }

    /// Opens the built-in editor (or REPS_EDITOR when set) under a plain-text header.
    public static string? Compose(Snippet snippet, string mode, IReadOnlyList<string> lines, string initial)
    {
        if (ExternalEditor.Command is not null)
        {
            var comment = string.Join("\n", lines.Select(l => "// " + l)) + "\n\n";
            var edited = ExternalEditor.Edit(comment + initial, ".cs", lines.Count + 2);
            if (edited is null)
            {
                return null;
            }
            edited = edited.Replace("\r\n", "\n");
            return edited.StartsWith(comment, StringComparison.Ordinal) ? edited[comment.Length..] : edited;
        }
        var header = new List<string> { $"{snippet.Id} {snippet.Title} · {mode}" };
        header.AddRange(lines);
        return new TextBox(header, initial).Run();
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
