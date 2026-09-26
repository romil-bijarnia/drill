using Spectre.Console;

namespace Reps;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var command = args.Length > 0 ? args[0].ToLowerInvariant() : "session";
        var rest = args.Skip(1).ToArray();
        try
        {
            return command switch
            {
                "session" or "go" or "today" => await SessionAsync(),
                "trace" => await PracticeAsync(Mode.Trace, rest),
                "recall" => await PracticeAsync(Mode.Recall, rest),
                "blank" => await PracticeAsync(Mode.Blank, rest),
                "list" or "ls" => List(rest),
                "stats" => Stats(),
                "due" => Due(),
                "verify" => await VerifyAsync(rest),
                "new" => New(rest),
                "import" => Import(rest),
                "where" => Where(),
                "help" or "-h" or "--help" => Help(),
                _ => Unknown(command),
            };
        }
        catch (RepsException error)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(error.Message)}[/]");
            return 1;
        }
    }

    // MARK: commands

    private static async Task<int> SessionAsync()
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var due = workspace.Store.Due(today)
            .Select(card => (card, snippet: workspace.Find(card.Id)))
            .Where(pair => pair.snippet is not null)
            .Take(15)
            .ToList();
        if (due.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]Nothing due today.[/] Extra reps: [cyan]reps trace <id>[/], [cyan]reps recall <id>[/], [cyan]reps blank <id>[/]. Scoreboard: [cyan]reps stats[/].");
            return 0;
        }
        AnsiConsole.MarkupLine($"[bold]{due.Count} due[/] · streak {workspace.Store.Streak(today)} days · Enter to start, q to stop between reps");
        var index = 0;
        foreach (var (card, snippet) in due)
        {
            index++;
            var mode = ModeFor(card.Box, snippet!);
            AnsiConsole.MarkupLine($"[grey]{index}/{due.Count}[/] {Markup.Escape(snippet!.Id)} {Markup.Escape(snippet.Title)} · box {card.Box} · [yellow]{mode.ToString().ToLowerInvariant()}[/]");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q')
            {
                break;
            }
            var result = await RunAsync(mode, snippet, workspace, today);
            Report(result, workspace, snippet, today);
        }
        workspace.Store.UpsertSession(today);
        Summary(workspace, today);
        return 0;
    }

    private static async Task<int> PracticeAsync(Mode mode, string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var snippet = args.Length > 0 ? workspace.Require(args[0]) : workspace.Random(mode);
        if (!snippet.Supports(mode))
        {
            throw new RepsException($"{snippet.Id} is not marked for {mode.ToString().ToLowerInvariant()} mode (modes: {string.Join(", ", snippet.Modes)})");
        }
        var result = await RunAsync(mode, snippet, workspace, today);
        Report(result, workspace, snippet, today);
        workspace.Store.UpsertSession(today);
        return result.Passed ? 0 : 1;
    }

    private static int List(string[] args)
    {
        using var workspace = Workspace.Open();
        var filter = args.Length > 0 ? args[0] : null;
        var today = Workspace.Today;
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("id").AddColumn("title").AddColumn("tags").AddColumn("box").AddColumn("due").AddColumn("last");
        foreach (var snippet in workspace.Snippets.Where(s => filter is null || s.Tags.Contains(filter) || s.Id == filter))
        {
            var card = workspace.Store.GetCard(snippet.Id);
            var recent = workspace.Store.RecentAttempts(snippet.Id, 3);
            var last = string.Join(" ", recent.Select(a => a.Passed ? "[green]●[/]" : "[red]●[/]"));
            var due = card is null ? "" : card.NextDue <= today ? "[yellow]today[/]" : card.NextDue.ToString("dd MMM");
            table.AddRow(Markup.Escape(snippet.Id), Markup.Escape(snippet.Title), Markup.Escape(string.Join(",", snippet.Tags)),
                card?.Box.ToString() ?? "-", due, last);
        }
        AnsiConsole.Write(table);
        foreach (var problem in workspace.Problems)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(problem)}[/]");
        }
        return 0;
    }

    private static int Due()
    {
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var due = workspace.Store.Due(today).Where(c => workspace.Find(c.Id) is not null).ToList();
        AnsiConsole.MarkupLine(due.Count == 0 ? "[green]0 due[/]" : $"[bold]{due.Count} due[/]: " + string.Join(", ", due.Take(15).Select(c => c.Id)));
        return 0;
    }

    private static int Stats()
    {
        using var workspace = Workspace.Open();
        Summary(workspace, Workspace.Today);
        return 0;
    }

    private static async Task<int> VerifyAsync(string[] args)
    {
        using var workspace = Workspace.Open();
        var failures = 0;
        var targets = args.Length > 0 ? workspace.Snippets.Where(s => args.Contains(s.Id)).ToList() : workspace.Snippets;
        foreach (var snippet in targets)
        {
            if (snippet.Tests.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]{snippet.Id}[/] no tests");
                continue;
            }
            var result = await Evaluator.RunAsync(snippet.Code, snippet.Tests, TimeSpan.FromSeconds(10));
            if (result.AllPassed)
            {
                AnsiConsole.MarkupLine($"[green]{snippet.Id}[/] {Markup.Escape(snippet.Title)} · {result.Outcomes.Count} tests");
                continue;
            }
            failures++;
            AnsiConsole.MarkupLine($"[red]{snippet.Id}[/] {Markup.Escape(snippet.Title)}");
            foreach (var error in result.CompileErrors)
            {
                AnsiConsole.MarkupLine($"    [red]{Markup.Escape(error)}[/]");
            }
            foreach (var outcome in result.Outcomes.Where(o => !o.Passed))
            {
                AnsiConsole.MarkupLine($"    [red]{Markup.Escape(outcome.Test.Call)}[/] expected [green]{Markup.Escape(outcome.Test.Expect)}[/] got [red]{Markup.Escape(outcome.Actual)}[/]");
            }
        }
        foreach (var problem in workspace.Problems)
        {
            failures++;
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(problem)}[/]");
        }
        AnsiConsole.MarkupLine(failures == 0 ? $"[green]{targets.Count} snippets verified[/]" : $"[red]{failures} problems[/]");
        return failures == 0 ? 0 : 1;
    }

    private static int New(string[] args)
    {
        if (args.Length < 2)
        {
            throw new RepsException("usage: reps new <id> <title words...>");
        }
        var root = Workspace.Root();
        var id = args[0];
        var title = string.Join(" ", args.Skip(1));
        var slug = string.Concat(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
        var path = Path.Combine(root, "snippets", $"{id}-{slug}.md");
        if (File.Exists(path))
        {
            throw new RepsException($"{path} already exists");
        }
        File.WriteAllText(path, SnippetLoader.Template(id, title));
        AnsiConsole.MarkupLine($"wrote [cyan]{Markup.Escape(path)}[/] — fill in spec, tests and reference code, then [cyan]reps verify {id}[/]");
        return 0;
    }

    /// Pulls static methods out of one of your own .cs files into trace/recall snippets,
    /// so you rehearse code you actually write. No tests are generated; add them by hand
    /// to make a snippet eligible for blank mode.
    private static int Import(string[] args)
    {
        if (args.Length == 0)
        {
            throw new RepsException("usage: reps import <file.cs> [tag ...]");
        }
        var file = args[0];
        if (!File.Exists(file))
        {
            throw new RepsException($"{file} does not exist");
        }
        var tags = args.Length > 1 ? args.Skip(1).ToList() : ["imported"];
        var root = Workspace.Root();
        var directory = Path.Combine(root, "snippets");
        Directory.CreateDirectory(directory);
        var existing = SnippetLoader.LoadAll(directory).Snippets;
        var nextId = existing.Select(s => int.TryParse(s.Id, out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
        var written = 0;
        foreach (var (name, code) in MethodExtractor.Extract(File.ReadAllText(file)))
        {
            if (existing.Any(s => s.Code == code))
            {
                continue;
            }
            var id = nextId.ToString("000");
            nextId++;
            var slug = string.Concat(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
            var path = Path.Combine(directory, $"{id}-{slug}.md");
            var content = $"---\nid: {id}\ntitle: {name}\ntags: [{string.Join(", ", tags)}]\nmodes: [trace, recall]\nspec: {name} from {Path.GetFileName(file)}\n---\n{code}\n";
            File.WriteAllText(path, content);
            AnsiConsole.MarkupLine($"[green]{id}[/] {Markup.Escape(name)} → {Markup.Escape(Path.GetFileName(path))}");
            written++;
        }
        AnsiConsole.MarkupLine(written == 0 ? "[yellow]no new static methods found[/]" : $"{written} snippet{(written == 1 ? "" : "s")} added (trace and recall). Add tests to a file and run [cyan]reps verify[/] to unlock blank mode.");
        return 0;
    }

    private static int Where()
    {
        var root = Workspace.Root();
        AnsiConsole.MarkupLine($"root     {Markup.Escape(root)}");
        AnsiConsole.MarkupLine($"snippets {Markup.Escape(Path.Combine(root, "snippets"))}");
        AnsiConsole.MarkupLine($"database {Markup.Escape(Path.Combine(root, "reps.db"))}");
        AnsiConsole.MarkupLine($"editor   {Markup.Escape(ExternalEditor.Command ?? "built-in (set REPS_EDITOR to use nano, vim or code --wait)")}");
        return 0;
    }

    private static int Help()
    {
        AnsiConsole.MarkupLine("""
            [bold]reps[/] — a gym for writing C# from a blank file

              reps                 today's session: everything due, capped at 15
              reps trace [id]      type over the reference (box 1 work)
              reps recall [id]     see it, then type it from memory (box 2)
              reps blank [id]      spec and tests only; write it, tests run (box 3+)
              reps list [tag]      every snippet with its box, due date and last results
              reps due             how many are due today
              reps stats           streak, boxes, weekly blank first-try rate
              reps verify [id...]  check reference code passes its own tests
              reps new <id> <title> start a new snippet file
              reps import <file.cs> [tags]  turn your own static methods into snippets
              reps where           paths and editor in use

            Pass: trace ≥ 97% accuracy · recall exact after whitespace · blank all tests.
            Pass moves a snippet up a box (due in 1, 2, 4, 8, 16 days); fail drops it to box 1.
            The number that matters is the blank-mode first-try rate, week over week.
            Recall and blank use a built-in editor: Ctrl+D submits, Esc gives up, Tab indents.
            Set REPS_EDITOR (nano, vim, "code --wait") only if you really want an external one.
            """);
        return 0;
    }

    private static int Unknown(string command)
    {
        AnsiConsole.MarkupLine($"[red]unknown command '{Markup.Escape(command)}'[/]. Try [cyan]reps help[/].");
        return 2;
    }

    // MARK: helpers

    private static Mode ModeFor(int box, Snippet snippet)
    {
        var wanted = box switch { 1 => Mode.Trace, 2 => Mode.Recall, _ => Mode.Blank };
        if (snippet.Supports(wanted))
        {
            return wanted;
        }
        if (wanted == Mode.Blank && snippet.Supports(Mode.Recall))
        {
            return Mode.Recall;
        }
        return snippet.Supports(Mode.Trace) ? Mode.Trace : Mode.Recall;
    }

    private static async Task<AttemptResult> RunAsync(Mode mode, Snippet snippet, Workspace workspace, DateOnly today)
    {
        var result = mode switch
        {
            Mode.Trace => TraceMode.Run(snippet),
            Mode.Recall => RecallMode.Run(snippet),
            _ => await BlankMode.RunAsync(snippet),
        };
        var firstTry = mode != Mode.Blank || (result.Tries == 1 && workspace.Store.BlankTriesToday(snippet.Id, today) == 0);
        workspace.Store.RecordAttempt(new AttemptRow(snippet.Id, mode, DateTime.Now, result.Seconds, result.Accuracy, result.Passed, firstTry));
        return result;
    }

    private static void Report(AttemptResult result, Workspace workspace, Snippet snippet, DateOnly today)
    {
        var before = workspace.Store.GetCard(snippet.Id)?.Box ?? 1;
        var (box, due) = workspace.Store.Advance(snippet.Id, result.Passed, today);
        var verdict = result.Passed ? "[green bold]PASS[/]" : "[red bold]FAIL[/]";
        var days = due.DayNumber - today.DayNumber;
        AnsiConsole.MarkupLine($"{verdict}  {Markup.Escape(result.Note)}  ·  box {before} → {box}, back in {days} day{(days == 1 ? "" : "s")}");
    }

    private static void Summary(Workspace workspace, DateOnly today)
    {
        var store = workspace.Store;
        var todayAttempts = store.AttemptsOn(today);
        var rate = Store.BlankFirstTryRate(todayAttempts);
        var cards = store.AllCards().Where(c => workspace.Find(c.Id) is not null).ToList();
        var boxes = string.Join("  ", Enumerable.Range(1, Store.MaxBox).Select(b => $"box{b} {cards.Count(c => c.Box == b)}"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]today[/] {todayAttempts.Count} reps · {todayAttempts.Count(a => a.Passed)} passed · blank first-try {(rate is null ? "—" : rate.Value.ToString("P0"))}");
        AnsiConsole.MarkupLine($"[bold]streak[/] {store.Streak(today)} days · [bold]due tomorrow[/] {cards.Count(c => c.NextDue <= today.AddDays(1) && c.NextDue > today)} · {boxes}");
        var weeks = store.Weekly(8, today);
        if (weeks.Any(w => w.Attempts > 0))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]blank first-try rate by week[/]");
            foreach (var week in weeks)
            {
                var bar = week.Rate is null ? "[grey]no blank reps[/]" : $"[green]{new string('█', (int)Math.Round(week.Rate.Value * 30))}[/][grey]{new string('░', 30 - (int)Math.Round(week.Rate.Value * 30))}[/] {week.Rate.Value:P0} ({week.BlankFirstTryPasses}/{week.BlankSnippets})";
                AnsiConsole.MarkupLine($"  {week.WeekStart:dd MMM}  {bar}  [grey]{week.Attempts} reps[/]");
            }
        }
    }

    private static void RequireTerminal()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            throw new RepsException("reps needs a real terminal for practice modes");
        }
    }
}

public sealed class RepsException(string message) : Exception(message);

/// The repo root (snippets/ and reps.db live there), the loaded snippet bank and the store.
public sealed class Workspace : IDisposable
{
    public string RootPath { get; }
    public List<Snippet> Snippets { get; }
    public List<string> Problems { get; }
    public Store Store { get; }

    private Workspace(string root)
    {
        RootPath = root;
        (Snippets, Problems) = SnippetLoader.LoadAll(Path.Combine(root, "snippets"));
        Store = new Store(Path.Combine(root, "reps.db"));
        Store.Sync(Snippets, Today);
    }

    public static Workspace Open()
    {
        var root = Root();
        var workspace = new Workspace(root);
        if (workspace.Snippets.Count == 0)
        {
            workspace.Dispose();
            throw new RepsException($"no snippets found in {Path.Combine(root, "snippets")}");
        }
        foreach (var problem in workspace.Problems)
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(problem)}[/]");
        }
        return workspace;
    }

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    public static string Root()
    {
        var configured = Environment.GetEnvironmentVariable("REPS_HOME");
        if (!string.IsNullOrEmpty(configured))
        {
            return configured;
        }
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "snippets")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "reps");
    }

    public Snippet? Find(string id) => Snippets.FirstOrDefault(s => s.Id == id);

    public Snippet Require(string id) => Find(id) ?? throw new RepsException($"no snippet with id {id}. Try: reps list");

    public Snippet Random(Mode mode)
    {
        var candidates = Snippets.Where(s => s.Supports(mode)).ToList();
        if (candidates.Count == 0)
        {
            throw new RepsException($"no snippets support {mode.ToString().ToLowerInvariant()} mode");
        }
        return candidates[System.Random.Shared.Next(candidates.Count)];
    }

    public void Dispose() => Store.Dispose();
}
