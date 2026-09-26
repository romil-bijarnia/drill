using Spectre.Console;

namespace Reps;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var command = args.Length > 0 ? args[0].ToLowerInvariant() : "session";
        var rest = args.Skip(1).ToArray();
        if (args.Length > 0 && Languages.TryParse(args[0], out _))
        {
            command = "session";
            rest = args;
        }
        try
        {
            return command switch
            {
                "session" or "go" or "today" => await SessionAsync(rest),
                "trace" => await PracticeAsync(Mode.Trace, rest),
                "recall" => await PracticeAsync(Mode.Recall, rest),
                "blank" => await PracticeAsync(Mode.Blank, rest),
                "list" or "ls" => List(rest),
                "stats" => Stats(),
                "due" => Due(),
                "verify" => await VerifyAsync(rest),
                "grind" => await GrindAsync(rest),
                "interview" => await InterviewAsync(rest),
                "projects" => Projects(),
                "project" => Project(rest),
                "work" => Work(rest),
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

    private static async Task<int> SessionAsync(string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var language = LanguageFilter(args);
        var due = workspace.Store.Due(today)
            .Select(card => (card, snippet: workspace.Find(card.Id)))
            .Where(pair => pair.snippet is not null && (language is null || pair.snippet.Language == language))
            .Take(15)
            .ToList();
        if (due.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]Nothing due today.[/] Keep going with [cyan]reps grind[/], a timed rep with [cyan]reps interview[/], or a project block with [cyan]reps work[/]. Scoreboard: [cyan]reps stats[/].");
            ProjectsLine(workspace);
            return 0;
        }
        var scope = language is null ? "" : $" · {language.Value.DisplayName()}";
        AnsiConsole.MarkupLine($"[bold]{due.Count} due[/]{scope} · streak {workspace.Store.Streak(today)} days · Enter to start, q to stop between reps");
        ProjectsLine(workspace);
        var index = 0;
        foreach (var (card, snippet) in due)
        {
            index++;
            var mode = ModeFor(card.Box, snippet!);
            AnsiConsole.MarkupLine($"[grey]{index}/{due.Count}[/] {Markup.Escape(snippet!.Id)} {Markup.Escape(snippet.Title)} [grey]{snippet.Language.DisplayName()}[/] · box {card.Box} · [yellow]{mode.ToString().ToLowerInvariant()}[/]");
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
        AnsiConsole.MarkupLine("[grey]Still going? [cyan]reps grind[/] never runs out.[/]");
        return 0;
    }

    /// Endless reps for the days you want to grind: due snippets first, then the weakest
    /// boxes, each in the mode its box calls for, until you press q.
    private static async Task<int> GrindAsync(string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var language = LanguageFilter(args);
        var pool = workspace.Snippets.Where(s => language is null || s.Language == language).ToList();
        if (pool.Count == 0)
        {
            throw new RepsException("no snippets to grind");
        }
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var reps = 0;
        var passes = 0;
        var recent = new Queue<string>();
        AnsiConsole.MarkupLine($"[bold]grind[/]{(language is null ? "" : $" · {language.Value.DisplayName()}")} · Enter for the next rep, q to stop");
        while (true)
        {
            var snippet = NextForGrind(workspace, pool, recent, today);
            var card = workspace.Store.GetCard(snippet.Id);
            var mode = ModeFor(card?.Box ?? 1, snippet);
            AnsiConsole.MarkupLine($"[grey]rep {reps + 1}[/] {Markup.Escape(snippet.Id)} {Markup.Escape(snippet.Title)} [grey]{snippet.Language.DisplayName()}[/] · box {card?.Box ?? 1} · [yellow]{mode.ToString().ToLowerInvariant()}[/]");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q' || key.Key == ConsoleKey.Escape)
            {
                break;
            }
            var result = await RunAsync(mode, snippet, workspace, today);
            Report(result, workspace, snippet, today);
            reps++;
            if (result.Passed) passes++;
            recent.Enqueue(snippet.Id);
            if (recent.Count > 8) recent.Dequeue();
            AnsiConsole.MarkupLine($"[grey]{reps} reps · {passes} passed · {clock.Elapsed:hh\\:mm\\:ss}[/]");
        }
        workspace.Store.UpsertSession(today);
        Summary(workspace, today);
        return 0;
    }

    private static Snippet NextForGrind(Workspace workspace, List<Snippet> pool, Queue<string> recent, DateOnly today)
    {
        var cards = workspace.Store.AllCards().ToDictionary(c => c.Id);
        var ranked = pool
            .Where(s => !recent.Contains(s.Id) || pool.Count <= recent.Count)
            .OrderBy(s => cards.TryGetValue(s.Id, out var c) && c.NextDue <= today ? 0 : 1)
            .ThenBy(s => cards.TryGetValue(s.Id, out var c) ? c.Box : 1)
            .ThenBy(s => workspace.Store.RecentAttempts(s.Id, 1).FirstOrDefault()?.StartedAt ?? DateTime.MinValue)
            .ThenBy(_ => System.Random.Shared.Next())
            .ToList();
        return ranked[0];
    }

    /// Interview mode: one blank-mode problem, 45 minutes by default, one submission.
    private static async Task<int> InterviewAsync(string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var language = LanguageFilter(args);
        var minutes = args.Select(a => int.TryParse(a, out var n) ? n : 0).FirstOrDefault(n => n > 0);
        var id = args.FirstOrDefault(a => !Languages.TryParse(a, out _) && !int.TryParse(a, out _));
        var snippet = id is not null
            ? workspace.Require(id)
            : workspace.Snippets.Where(s => s.Supports(Mode.Blank) && Runner.Available(s.Language) && (language is null || s.Language == language))
                .OrderBy(_ => System.Random.Shared.Next()).FirstOrDefault()
              ?? throw new RepsException("no snippet with tests to interview on");
        if (!snippet.Supports(Mode.Blank) || !Runner.Available(snippet.Language))
        {
            throw new RepsException($"{snippet.Id} cannot run in blank mode here");
        }
        var limit = TimeSpan.FromMinutes(minutes == 0 ? 45 : minutes);
        AnsiConsole.MarkupLine($"[bold]interview[/] · {Markup.Escape(snippet.Id)} · {limit.TotalMinutes:0} minutes · Enter to start");
        Console.ReadKey(intercept: true);
        var result = await BlankMode.RunAsync(snippet, limit);
        workspace.Store.RecordAttempt(new AttemptRow(snippet.Id, Mode.Blank, DateTime.Now, result.Seconds, null, result.Passed, result.Tries == 1 && workspace.Store.BlankTriesToday(snippet.Id, today) == 0));
        Report(result, workspace, snippet, today);
        workspace.Store.UpsertSession(today);
        return result.Passed ? 0 : 1;
    }

    // MARK: projects

    private static int Projects()
    {
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var weekStart = today.AddDays(-6);
        if (workspace.Projects.Count == 0)
        {
            AnsiConsole.MarkupLine("No projects yet. [cyan]reps project new <id> <title>[/] creates one under projects/.");
            return 0;
        }
        var table = new Table().Border(TableBorder.Rounded).AddColumn("id").AddColumn("title").AddColumn("status").AddColumn("steps").AddColumn("next").AddColumn("7 days");
        foreach (var project in workspace.Projects)
        {
            var hours = workspace.Store.WorkSeconds(weekStart, today, project.Id) / 3600;
            table.AddRow(Markup.Escape(project.Id), Markup.Escape(project.Title), project.Status,
                $"{project.DoneCount}/{project.Steps.Count}",
                Markup.Escape(project.NextStep?.Text ?? "done"),
                hours > 0 ? $"{hours:0.0} h" : "");
        }
        AnsiConsole.Write(table);
        foreach (var problem in workspace.ProjectProblems)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(problem)}[/]");
        }
        return 0;
    }

    private static int Project(string[] args)
    {
        if (args.Length >= 3 && args[0] == "new")
        {
            var root = Workspace.Root();
            var directory = Path.Combine(root, "projects");
            Directory.CreateDirectory(directory);
            var id = args[1];
            var path = Path.Combine(directory, $"{id}.md");
            if (File.Exists(path))
            {
                throw new RepsException($"{path} already exists");
            }
            File.WriteAllText(path, ProjectLoader.Template(id, string.Join(" ", args.Skip(2))));
            AnsiConsole.MarkupLine($"wrote [cyan]{Markup.Escape(path)}[/] — replace the goal and the steps, then [cyan]reps work {id}[/]");
            return 0;
        }
        if (args.Length == 0)
        {
            return Projects();
        }
        using var workspace = Workspace.Open();
        var project = workspace.RequireProject(args[0]);
        var today = Workspace.Today;
        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(project.Id)}[/] [bold]{Markup.Escape(project.Title)}[/]  [grey]{project.Status}{(project.Stack.Count > 0 ? " · " + string.Join(", ", project.Stack) : "")}[/]");
        if (project.Goal.Length > 0)
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(project.Goal)}[/]");
        }
        foreach (var step in project.Steps)
        {
            AnsiConsole.MarkupLine(step.Done ? $"  [green]✓[/] [grey]{Markup.Escape(step.Text)}[/]" : $"  [yellow]{step.Number,2}[/] {Markup.Escape(step.Text)}");
        }
        var todaySeconds = workspace.Store.WorkSeconds(today, today, project.Id);
        var weekSeconds = workspace.Store.WorkSeconds(today.AddDays(-6), today, project.Id);
        AnsiConsole.MarkupLine($"[grey]today {todaySeconds / 60:0} min · last 7 days {weekSeconds / 3600:0.0} h · {project.DoneCount}/{project.Steps.Count} steps[/]");
        return 0;
    }

    private static int Work(string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var id = args.FirstOrDefault(a => !int.TryParse(a, out _));
        var project = id is not null
            ? workspace.RequireProject(id)
            : workspace.Projects.FirstOrDefault(p => p.IsActive && p.NextStep is not null)
              ?? throw new RepsException("no active project with open steps; reps project new <id> <title>");
        var stepNumber = args.Select(a => int.TryParse(a, out var n) ? n : 0).FirstOrDefault(n => n > 0);
        while (true)
        {
            var step = stepNumber > 0
                ? project.Steps.FirstOrDefault(s => s.Number == stepNumber) ?? throw new RepsException($"no step {stepNumber}")
                : project.NextStep;
            if (step is null)
            {
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(project.Title)} is done.[/] Mark it status: done in the file, or add the next steps.");
                return 0;
            }
            var startedAt = DateTime.Now;
            var outcome = WorkBlock.Run(project, step, workspace.Store.WorkSeconds(today, today, project.Id));
            workspace.Store.LogWork(project.Id, step.Number, startedAt, outcome.Seconds, outcome.Completed);
            if (outcome.Completed)
            {
                ProjectLoader.MarkDone(project, step.Number);
                project = ProjectLoader.Parse(File.ReadAllText(project.Path), project.Path);
                AnsiConsole.MarkupLine($"[green bold]step {step.Number} done[/] in {outcome.Seconds / 60:0} min · {project.DoneCount}/{project.Steps.Count}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]paused[/] step {step.Number} after {outcome.Seconds / 60:0} min · it stays open");
            }
            stepNumber = 0;
            if (outcome.Quit || project.NextStep is null)
            {
                break;
            }
            AnsiConsole.MarkupLine($"[grey]next: {Markup.Escape(project.NextStep.Text)} · Enter to start it, q to stop[/]");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q' || key.Key == ConsoleKey.Escape)
            {
                break;
            }
        }
        var total = workspace.Store.WorkSeconds(today, today);
        AnsiConsole.MarkupLine($"[bold]project work today[/] {total / 60:0} min");
        return 0;
    }

    private static void ProjectsLine(Workspace workspace)
    {
        var open = workspace.Projects.Where(p => p.IsActive && p.NextStep is not null).ToList();
        if (open.Count == 0)
        {
            return;
        }
        AnsiConsole.MarkupLine("[grey]projects:[/] " + string.Join(" · ", open.Take(4).Select(p => $"{Markup.Escape(p.Id)} ({p.Steps.Count - p.DoneCount} left)")) + " [grey]→ reps work <id>[/]");
    }

    private static async Task<int> PracticeAsync(Mode mode, string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var language = LanguageFilter(args);
        var id = args.FirstOrDefault(a => !Languages.TryParse(a, out _));
        var snippet = id is not null ? workspace.Require(id) : workspace.Random(mode, language);
        if (!snippet.Supports(mode))
        {
            throw new RepsException($"{snippet.Id} is not marked for {mode.ToString().ToLowerInvariant()} mode (modes: {string.Join(", ", snippet.Modes)}{(snippet.Tests.Count == 0 ? ", no tests" : "")})");
        }
        if (mode == Mode.Blank && !Runner.Available(snippet.Language))
        {
            throw new RepsException($"blank mode for {snippet.Language.DisplayName()} needs {(snippet.Language == Language.Python ? "python3" : "cc (Xcode command line tools)")} on this machine");
        }
        var result = await RunAsync(mode, snippet, workspace, today);
        Report(result, workspace, snippet, today);
        workspace.Store.UpsertSession(today);
        return result.Passed ? 0 : 1;
    }

    private static int List(string[] args)
    {
        using var workspace = Workspace.Open();
        var language = LanguageFilter(args);
        var filter = args.FirstOrDefault(a => !Languages.TryParse(a, out _));
        var today = Workspace.Today;
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("id").AddColumn("title").AddColumn("lang").AddColumn("tags").AddColumn("box").AddColumn("due").AddColumn("last");
        foreach (var snippet in workspace.Snippets.Where(s => (language is null || s.Language == language) && (filter is null || s.Tags.Contains(filter) || s.Id == filter)))
        {
            var card = workspace.Store.GetCard(snippet.Id);
            var recent = workspace.Store.RecentAttempts(snippet.Id, 3);
            var last = string.Join(" ", recent.Select(a => a.Passed ? "[green]●[/]" : "[red]●[/]"));
            var due = card is null ? "" : card.NextDue <= today ? "[yellow]today[/]" : card.NextDue.ToString("dd MMM");
            table.AddRow(Markup.Escape(snippet.Id), Markup.Escape(snippet.Title), snippet.Language.DisplayName(), Markup.Escape(string.Join(",", snippet.Tags)),
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
        var language = LanguageFilter(args);
        var ids = args.Where(a => !Languages.TryParse(a, out _)).ToList();
        var targets = workspace.Snippets.Where(s => (language is null || s.Language == language) && (ids.Count == 0 || ids.Contains(s.Id))).ToList();
        foreach (var snippet in targets)
        {
            if (snippet.Tests.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]{snippet.Id}[/] no tests (trace and recall only)");
                continue;
            }
            if (!Runner.Available(snippet.Language))
            {
                failures++;
                AnsiConsole.MarkupLine($"[red]{snippet.Id}[/] no toolchain for {snippet.Language.DisplayName()}");
                continue;
            }
            var result = await Runner.EvaluateAsync(snippet, snippet.Code, TimeSpan.FromSeconds(20));
            if (result.AllPassed)
            {
                AnsiConsole.MarkupLine($"[green]{snippet.Id}[/] {Markup.Escape(snippet.Title)} [grey]{snippet.Language.DisplayName()}[/] · {result.Outcomes.Count} tests");
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
            throw new RepsException("usage: reps new <lang-id> <title words...>   e.g. reps new py-031 Flatten a list");
        }
        var root = Workspace.Root();
        var id = args[0];
        if (!Languages.TryParse(id.Split('-')[0], out var language))
        {
            throw new RepsException("ids start with the language: cs-, py-, c- or asm-");
        }
        var title = string.Join(" ", args.Skip(1));
        var slug = string.Concat(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
        var path = Path.Combine(root, "snippets", $"{id}-{slug}.md");
        if (File.Exists(path))
        {
            throw new RepsException($"{path} already exists");
        }
        File.WriteAllText(path, SnippetLoader.Template(id, title, language));
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
        var language = Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".py" => Language.Python,
            ".c" or ".h" => Language.C,
            ".s" or ".asm" => Language.Asm,
            ".cs" => Language.CSharp,
            _ => throw new RepsException("import understands .cs, .py, .c and .s files"),
        };
        var root = Workspace.Root();
        var directory = Path.Combine(root, "snippets");
        Directory.CreateDirectory(directory);
        var existing = SnippetLoader.LoadAll(directory).Snippets;
        var prefix = language.Key() + "-";
        var nextId = existing.Where(s => s.Id.StartsWith(prefix, StringComparison.Ordinal))
            .Select(s => int.TryParse(s.Id[prefix.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
        var written = 0;
        foreach (var (name, code) in MethodExtractor.Extract(File.ReadAllText(file), language))
        {
            if (existing.Any(s => s.Code == code))
            {
                continue;
            }
            var id = prefix + nextId.ToString("000");
            nextId++;
            var slug = string.Concat(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
            var path = Path.Combine(directory, $"{id}-{slug}.md");
            var content = $"---\nid: {id}\ntitle: {name}\nlang: {language.Key()}\ntags: [{string.Join(", ", tags)}]\nmodes: [trace, recall]\nspec: {name} from {Path.GetFileName(file)}\n---\n{code}\n";
            File.WriteAllText(path, content);
            AnsiConsole.MarkupLine($"[green]{id}[/] {Markup.Escape(name)} → {Markup.Escape(Path.GetFileName(path))}");
            written++;
        }
        AnsiConsole.MarkupLine(written == 0 ? "[yellow]no new functions found[/]" : $"{written} snippet{(written == 1 ? "" : "s")} added (trace and recall). Add tests to a file and run [cyan]reps verify[/] to unlock blank mode.");
        return 0;
    }

    private static int Where()
    {
        var root = Workspace.Root();
        AnsiConsole.MarkupLine($"root     {Markup.Escape(root)}");
        AnsiConsole.MarkupLine($"snippets {Markup.Escape(Path.Combine(root, "snippets"))}");
        AnsiConsole.MarkupLine($"database {Markup.Escape(Path.Combine(root, "reps.db"))}");
        AnsiConsole.MarkupLine($"editor   {Markup.Escape(ExternalEditor.Command ?? "built-in (set REPS_EDITOR to use nano, vim or code --wait)")}");
        foreach (var language in Languages.All)
        {
            var tool = Runner.Tool(language);
            AnsiConsole.MarkupLine($"{language.DisplayName(),-8} {(tool is null ? "[red]missing[/]" : Markup.Escape(tool))}");
        }
        return 0;
    }

    private static int Help()
    {
        AnsiConsole.WriteLine("""
            reps — a gym for writing code from a blank file: C#, Python, C, ARM64 asm

              reps [lang]          today's session: everything due, capped at 15
              reps trace [id|lang] type over the reference (box 1 work)
              reps recall [id|lang] see it, then type it from memory (box 2)
              reps blank [id|lang] spec and tests only; write it, tests run (box 3+)
              reps grind [lang]    endless reps, weakest first, until you press q
              reps interview [lang|id] [minutes]  one blank problem, 45 min default, one submission
              reps list [lang] [tag] every snippet with its box, due date and last results
              reps projects        real builds with steps: progress and hours
              reps project <id>    one project's goal, steps and time
              reps project new <id> <title>  a new project file under projects/
              reps work [id] [step] timed block on the next open step; Enter marks it done
              reps due             how many are due today
              reps stats           streak, boxes, weekly blank first-try rate
              reps verify [id...]  check reference code passes its own tests
              reps new <id> <title> start a new snippet file (id starts with cs-, py-, c- or asm-)
              reps import <file> [tags]  your own functions (.cs .py .c .s) as trace/recall snippets
              reps where           paths, editor and toolchains in use

            Languages: cs (C#, Roslyn in-process), py (python3), c (cc), asm (ARM64, assembled with cc
            and called from a C harness). lang can be given anywhere an id can.

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

    private static Language? LanguageFilter(string[] args)
    {
        foreach (var arg in args)
        {
            if (Languages.TryParse(arg, out var language))
            {
                return language;
            }
        }
        return null;
    }

    private static Mode ModeFor(int box, Snippet snippet)
    {
        var wanted = box switch { 1 => Mode.Trace, 2 => Mode.Recall, _ => Mode.Blank };
        if (snippet.Supports(wanted) && (wanted != Mode.Blank || Runner.Available(snippet.Language)))
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
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]today[/] {todayAttempts.Count} reps · {todayAttempts.Count(a => a.Passed)} passed · blank first-try {(rate is null ? "—" : rate.Value.ToString("P0"))}");
        AnsiConsole.MarkupLine($"[bold]streak[/] {store.Streak(today)} days · [bold]due tomorrow[/] {cards.Count(c => c.NextDue <= today.AddDays(1) && c.NextDue > today)}");
        var boxTable = new Table().Border(TableBorder.Simple).AddColumn("").AddColumn("due").AddColumn("box1").AddColumn("box2").AddColumn("box3").AddColumn("box4").AddColumn("box5");
        foreach (var language in Languages.All)
        {
            var mine = cards.Where(c => workspace.Find(c.Id)!.Language == language).ToList();
            if (mine.Count == 0)
            {
                continue;
            }
            boxTable.AddRow([language.DisplayName(), mine.Count(c => c.NextDue <= today).ToString(), .. Enumerable.Range(1, Store.MaxBox).Select(b => mine.Count(c => c.Box == b).ToString())]);
        }
        AnsiConsole.Write(boxTable);
        var workToday = store.WorkSeconds(today, today);
        var workWeek = store.WorkSeconds(today.AddDays(-6), today);
        if (workWeek > 0)
        {
            AnsiConsole.MarkupLine($"[bold]projects[/] today {workToday / 60:0} min · last 7 days {workWeek / 3600:0.0} h · {store.StepsCompleted(today.AddDays(-6), today)} steps done");
        }
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
    public List<Project> Projects { get; }
    public List<string> ProjectProblems { get; }
    public Store Store { get; }

    private Workspace(string root)
    {
        RootPath = root;
        (Snippets, Problems) = SnippetLoader.LoadAll(Path.Combine(root, "snippets"));
        (Projects, ProjectProblems) = ProjectLoader.LoadAll(Path.Combine(root, "projects"));
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

    public Project RequireProject(string id) =>
        Projects.FirstOrDefault(p => p.Id == id) ?? throw new RepsException($"no project with id {id}. Try: reps projects");

    public Snippet Random(Mode mode, Language? language = null)
    {
        var candidates = Snippets.Where(s => s.Supports(mode) && (language is null || s.Language == language)).ToList();
        if (candidates.Count == 0)
        {
            throw new RepsException($"no {(language is null ? "" : language.Value.DisplayName() + " ")}snippets support {mode.ToString().ToLowerInvariant()} mode");
        }
        return candidates[System.Random.Shared.Next(candidates.Count)];
    }

    public void Dispose() => Store.Dispose();
}
