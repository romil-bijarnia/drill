using Spectre.Console;

namespace Drill;

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
                "session" or "go" or "today" or "grind" => await FlowAsync(null, rest),
                "trace" => await PracticeAsync(Mode.Trace, rest),
                "recall" => await PracticeAsync(Mode.Recall, rest),
                "blank" => await PracticeAsync(Mode.Blank, rest),
                "predict" => await PracticeAsync(Mode.Predict, rest),
                "bits" => Bits(rest),
                "machine" => MachineCommand(rest),
                "compile" => await CompileAsync(rest),
                "list" or "ls" => List(rest),
                "stats" => Stats(),
                "verify" => await VerifyAsync(rest),
                "interview" => await InterviewAsync(rest),
                "down" => await DownAsync(rest),
                "families" => Families(),
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
        catch (DrillException error)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(error.Message)}[/]");
            return 1;
        }
    }

    // MARK: commands

    /// The session: one rep after another until you press q. Nothing is owed and there is
    /// no count to clear; the order simply puts first whatever is ready to come back, then
    /// the weakest, then the least recent, with no repeat inside the last eight. Each rep
    /// runs in the mode its box calls for, or in the one mode you asked for.
    private static async Task<int> FlowAsync(Mode? fixedMode, string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var language = LanguageFilter(args);
        var pool = workspace.Snippets
            .Where(s => language is null || s.Language == language)
            .Where(s => fixedMode is null || (s.Supports(fixedMode.Value) && (fixedMode != Mode.Blank || Runner.Available(s.Language))))
            .ToList();
        if (pool.Count == 0)
        {
            var scopeName = language is null ? "" : language.Value.DisplayName() + " ";
            throw new DrillException(fixedMode is null ? $"no {scopeName}snippets to practise" : $"no {scopeName}snippets can run in {fixedMode.Value.ToString().ToLowerInvariant()} mode here");
        }
        var title = fixedMode?.ToString().ToLowerInvariant() ?? "drill";
        var scope = language is null ? "" : $" · {language.Value.DisplayName()}";
        AnsiConsole.MarkupLine($"[bold]{title}[/]{scope} · streak {workspace.Store.Streak(today)} days · Enter for the next rep, q to stop");
        if (fixedMode is null)
        {
            AnsiConsole.MarkupLine("[grey]new or missed snippets come as a cold try from the spec; miss it and the reference follows, then you write it again[/]");
        }
        ProjectsLine(workspace);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var count = 0;
        var passes = 0;
        var recent = new Queue<string>();
        while (true)
        {
            var snippet = Next(workspace, pool, recent, today);
            var box = workspace.Store.GetCard(snippet.Id)?.Box ?? 1;
            var label = fixedMode is null ? Label(box, ModeFor(box, snippet)) : fixedMode.Value.ToString().ToLowerInvariant();
            AnsiConsole.MarkupLine($"[grey]rep {count + 1}[/] {Markup.Escape(snippet.Id)} {Markup.Escape(snippet.Title)} [grey]{snippet.Language.DisplayName()}[/] · [yellow]{label}[/]");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q' || key.Key == ConsoleKey.Escape)
            {
                break;
            }
            var result = fixedMode is null ? await RepAsync(snippet, box, workspace, today) : await RunAsync(fixedMode.Value, snippet, workspace, today);
            Report(result, workspace, snippet, today);
            count++;
            if (result.Passed) passes++;
            recent.Enqueue(snippet.Id);
            if (recent.Count > 8) recent.Dequeue();
            AnsiConsole.MarkupLine($"[grey]{count} reps · {passes} passed · {clock.Elapsed:hh\\:mm\\:ss}[/]");
        }
        workspace.Store.UpsertSession(today);
        Summary(workspace, today);
        return 0;
    }

    private static Snippet Next(Workspace workspace, List<Snippet> pool, Queue<string> recent, DateOnly today)
    {
        var cards = workspace.Store.AllCards().ToDictionary(c => c.Id);
        return pool
            .Where(s => !recent.Contains(s.Id) || pool.Count <= recent.Count)
            .OrderBy(s => cards.TryGetValue(s.Id, out var c) && c.NextDue <= today ? 0 : 1)
            .ThenBy(s => cards.TryGetValue(s.Id, out var c) ? c.Box : 1)
            .ThenBy(s => workspace.Store.RecentAttempts(s.Id, 1).FirstOrDefault()?.StartedAt ?? DateTime.MinValue)
            .ThenBy(_ => System.Random.Shared.Next())
            .First();
    }

    /// Down the stack: one problem in Python, then C#, then C, then ARM64. A language is
    /// done once its snippet has left box 1 (a pass in recall or blank), and the next one
    /// opens when the one above it passes. n moves to another problem, q stops.
    private static async Task<int> DownAsync(string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var families = workspace.Families();
        if (families.Count == 0)
        {
            throw new DrillException("no families yet: give snippets that solve the same problem a family: line in their header");
        }
        var chosen = args.FirstOrDefault(families.ContainsKey);
        if (args.Length > 0 && chosen is null)
        {
            throw new DrillException($"no family called {args[0]}. Try: drill families");
        }
        var skipped = new HashSet<string>();
        AnsiConsole.MarkupLine("[bold]down[/] · Python → C# → C → ARM64 · Enter for the next rep, n for another problem, q to stop");
        var count = 0;
        var passes = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            var family = chosen ?? NextFamily(workspace, families, skipped);
            if (family is null)
            {
                AnsiConsole.MarkupLine("[green]every problem is done all the way down.[/]");
                break;
            }
            var members = families[family];
            var next = members.FirstOrDefault(m => Level(workspace, m) < 2);
            if (next is null)
            {
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(family)}[/] is done all the way down.");
                if (chosen is not null)
                {
                    break;
                }
                skipped.Add(family);
                continue;
            }
            var done = members.TakeWhile(m => m != next).Select(m => m.Language.DisplayName()).ToList();
            var level = Level(workspace, next);
            var trail = done.Count == 0 ? "" : $"  [grey]done: {string.Join(", ", done)}[/]";
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(family)}[/] {Markup.Escape(next.Id)} {Markup.Escape(next.Title)} [grey]{next.Language.DisplayName()}[/] · [yellow]{Label(level, ModeFor(level, next))}[/]{trail}");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q' || key.Key == ConsoleKey.Escape)
            {
                break;
            }
            if (key.KeyChar is 'n' or 'N')
            {
                if (chosen is not null)
                {
                    break;
                }
                skipped.Add(family);
                continue;
            }
            var result = await RepAsync(next, level, workspace, today);
            Report(result, workspace, next, today);
            count++;
            if (result.Passed) passes++;
            AnsiConsole.MarkupLine($"[grey]{count} reps · {passes} passed · {clock.Elapsed:hh\\:mm\\:ss}[/]");
        }
        workspace.Store.UpsertSession(today);
        Summary(workspace, today);
        return 0;
    }

    private static int Level(Workspace workspace, Snippet snippet) => workspace.Store.GetCard(snippet.Id)?.Box ?? 1;

    /// The family furthest along that is not finished, so a problem gets taken all the way
    /// down before another one starts; ties broken at random.
    private static string? NextFamily(Workspace workspace, SortedDictionary<string, List<Snippet>> families, HashSet<string> skipped) =>
        families
            .Where(f => !skipped.Contains(f.Key))
            .Select(f => (f.Key, Done: f.Value.Count(m => Level(workspace, m) >= 2), Total: f.Value.Count))
            .Where(f => f.Done < f.Total)
            .OrderByDescending(f => f.Done)
            .ThenBy(_ => System.Random.Shared.Next())
            .Select(f => f.Key)
            .FirstOrDefault();

    private static int Families()
    {
        using var workspace = Workspace.Open();
        var families = workspace.Families();
        if (families.Count == 0)
        {
            AnsiConsole.MarkupLine("No families yet. Add [cyan]family: <name>[/] to the header of snippets that solve the same problem in different languages.");
            return 0;
        }
        var table = new Table().Border(TableBorder.Rounded).AddColumn("problem");
        foreach (var language in Languages.Stack)
        {
            table.AddColumn(language.DisplayName());
        }
        foreach (var (name, members) in families)
        {
            var cells = new List<string> { Markup.Escape(name) };
            foreach (var language in Languages.Stack)
            {
                var member = members.FirstOrDefault(m => m.Language == language);
                cells.Add(member is null ? "[grey]–[/]" : Level(workspace, member) >= 2
                    ? $"[green]✓[/] [grey]{Markup.Escape(member.Id)}[/]"
                    : $"[yellow]·[/] [grey]{Markup.Escape(member.Id)}[/]");
            }
            table.AddRow(cells.ToArray());
        }
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]✓ passed at least once · [cyan]drill down <problem>[/] takes one problem from Python to ARM64[/]");
        return 0;
    }

    private static int MachineCommand(string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        return MachineMode.Run(workspace.Store, args.FirstOrDefault());
    }

    private static int Bits(string[] args)
    {
        RequireTerminal();
        using var workspace = Workspace.Open();
        return BitsMode.Run(workspace.Store, args.FirstOrDefault());
    }

    /// Compile view: clang's ARM64 for a C snippet next to the C, then your own ARM64 for
    /// the same tests. An id gives one rep; a bare `drill compile` keeps going.
    private static async Task<int> CompileAsync(string[] args)
    {
        RequireTerminal();
        if (!Runner.Available(Language.C))
        {
            throw new DrillException("compile view needs cc (xcode-select --install)");
        }
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var id = args.FirstOrDefault(a => !Languages.TryParse(a, out _));
        var pool = workspace.Snippets.Where(s => s.Language == Language.C && s.Tests.Count > 0).ToList();
        if (pool.Count == 0)
        {
            throw new DrillException("no C snippets with tests");
        }
        var recent = new Queue<string>();
        var count = 0;
        var passes = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        AnsiConsole.MarkupLine("[bold]compile[/] · C → clang → your ARM64 · Enter for the next one, q to stop");
        while (true)
        {
            var snippet = id is not null ? workspace.Require(id) : Next(workspace, pool, recent, today);
            if (snippet.Language != Language.C || snippet.Tests.Count == 0)
            {
                throw new DrillException($"{snippet.Id} is not a C snippet with tests");
            }
            AnsiConsole.MarkupLine($"[grey]rep {count + 1}[/] {Markup.Escape(snippet.Id)} {Markup.Escape(snippet.Title)} [grey]C → ARM64[/]");
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q' || key.Key == ConsoleKey.Escape)
            {
                break;
            }
            var listing = await AnsiConsole.Status().StartAsync("asking clang…", _ => CompileView.Assemble(snippet.Code));
            Console.Clear();
            Ui.Heading(snippet, "compile");
            AnsiConsole.Write(new Panel(new Text(snippet.Code)).Border(BoxBorder.Rounded).Header("[grey] C [/]"));
            AnsiConsole.Write(new Panel(new Text(listing)).Border(BoxBorder.Rounded).Header("[grey] clang -O1 [/]"));
            AnsiConsole.MarkupLine("[grey]Read both. Enter to write your own ARM64 for the same tests, Esc to skip.[/]");
            var go = Console.ReadKey(intercept: true);
            if (go.Key == ConsoleKey.Escape)
            {
                continue;
            }
            var decl = CompileView.Prototypes(snippet.Code);
            var target = snippet with { Id = snippet.Id + "@asm", Language = Language.Asm, Decl = decl, Modes = ["blank"], Code = listing };
            var result = await BlankMode.RunAsync(target);
            workspace.Store.Sync([target], today);
            workspace.Store.RecordAttempt(new AttemptRow(target.Id, Mode.Blank, DateTime.Now, result.Seconds, null, result.Passed, result.Tries == 1));
            AnsiConsole.MarkupLine($"{(result.Passed ? "[green bold]PASS[/]" : "[red bold]FAIL[/]")}  {Markup.Escape(result.Note)}");
            count++;
            if (result.Passed) passes++;
            AnsiConsole.MarkupLine($"[grey]{count} reps · {passes} passed · {clock.Elapsed:hh\\:mm\\:ss}[/]");
            if (id is not null)
            {
                break;
            }
            recent.Enqueue(snippet.Id);
            if (recent.Count > 8) recent.Dequeue();
        }
        workspace.Store.UpsertSession(today);
        return 0;
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
              ?? throw new DrillException("no snippet with tests to interview on");
        if (!snippet.Supports(Mode.Blank) || !Runner.Available(snippet.Language))
        {
            throw new DrillException($"{snippet.Id} cannot run in blank mode here");
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
            AnsiConsole.MarkupLine("No projects yet. [cyan]drill project new <id> <title>[/] creates one under projects/.");
            return 0;
        }
        var table = new Table().Border(TableBorder.Rounded).AddColumn("id").AddColumn("title").AddColumn("status").AddColumn("steps").AddColumn("next").AddColumn("7 days");
        foreach (var project in workspace.Projects.Where(p => p.IsActive || p.DoneCount > 0))
        {
            var hours = workspace.Store.WorkSeconds(weekStart, today, project.Id) / 3600;
            table.AddRow(Markup.Escape(project.Id), Markup.Escape(project.Title), project.Status,
                $"{project.DoneCount}/{project.Steps.Count}",
                Markup.Escape(project.NextStep?.Text ?? "done"),
                hours > 0 ? $"{hours:0.0} h" : "");
        }
        AnsiConsole.Write(table);
        var parked = workspace.Projects.Where(p => !p.IsActive && p.DoneCount == 0).ToList();
        if (parked.Count > 0)
        {
            AnsiConsole.MarkupLine($"[grey]later: {string.Join(", ", parked.Select(p => Markup.Escape(p.Id)))} · set status: active in the file to start one[/]");
        }
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
                throw new DrillException($"{path} already exists");
            }
            File.WriteAllText(path, ProjectLoader.Template(id, string.Join(" ", args.Skip(2))));
            AnsiConsole.MarkupLine($"wrote [cyan]{Markup.Escape(path)}[/] — replace the goal and the steps, then [cyan]drill work {id}[/]");
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
              ?? throw new DrillException("no active project with open steps; drill project new <id> <title>");
        var stepNumber = args.Select(a => int.TryParse(a, out var n) ? n : 0).FirstOrDefault(n => n > 0);
        while (true)
        {
            var step = stepNumber > 0
                ? project.Steps.FirstOrDefault(s => s.Number == stepNumber) ?? throw new DrillException($"no step {stepNumber}")
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
        AnsiConsole.MarkupLine("[grey]projects:[/] " + string.Join(" · ", open.Take(4).Select(p => $"{Markup.Escape(p.Id)} ({p.Steps.Count - p.DoneCount} left)")) + " [grey]→ drill work <id>[/]");
    }

    /// One snippet in a chosen mode when an id is given; otherwise the session in that mode.
    private static async Task<int> PracticeAsync(Mode mode, string[] args)
    {
        var id = args.FirstOrDefault(a => !Languages.TryParse(a, out _));
        if (id is null)
        {
            return await FlowAsync(mode, args);
        }
        RequireTerminal();
        using var workspace = Workspace.Open();
        var today = Workspace.Today;
        var snippet = workspace.Require(id);
        if (!snippet.Supports(mode))
        {
            throw new DrillException($"{snippet.Id} is not marked for {mode.ToString().ToLowerInvariant()} mode (modes: {string.Join(", ", snippet.Modes)}{(snippet.Tests.Count == 0 ? ", no tests" : "")})");
        }
        if (mode == Mode.Blank && !Runner.Available(snippet.Language))
        {
            throw new DrillException($"blank mode for {snippet.Language.DisplayName()} needs {(snippet.Language == Language.Python ? "python3" : "cc (Xcode command line tools)")} on this machine");
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
            .AddColumn("id").AddColumn("title").AddColumn("lang").AddColumn("tags").AddColumn("box").AddColumn("next").AddColumn("last");
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
            throw new DrillException("usage: drill new <lang-id> <title words...>   e.g. drill new py-031 Flatten a list");
        }
        var root = Workspace.Root();
        var id = args[0];
        if (!Languages.TryParse(id.Split('-')[0], out var language))
        {
            throw new DrillException("ids start with the language: cs-, py-, c- or asm-");
        }
        var title = string.Join(" ", args.Skip(1));
        var slug = string.Concat(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
        var path = Path.Combine(root, "snippets", $"{id}-{slug}.md");
        if (File.Exists(path))
        {
            throw new DrillException($"{path} already exists");
        }
        File.WriteAllText(path, SnippetLoader.Template(id, title, language));
        AnsiConsole.MarkupLine($"wrote [cyan]{Markup.Escape(path)}[/] — fill in spec, tests and reference code, then [cyan]drill verify {id}[/]");
        return 0;
    }

    /// Pulls static methods out of one of your own .cs files into trace/recall snippets,
    /// so you rehearse code you actually write. No tests are generated; add them by hand
    /// to make a snippet eligible for blank mode.
    private static int Import(string[] args)
    {
        if (args.Length == 0)
        {
            throw new DrillException("usage: drill import <file.cs> [tag ...]");
        }
        var file = args[0];
        if (!File.Exists(file))
        {
            throw new DrillException($"{file} does not exist");
        }
        var tags = args.Length > 1 ? args.Skip(1).ToList() : ["imported"];
        var language = Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".py" => Language.Python,
            ".c" or ".h" => Language.C,
            ".s" or ".asm" => Language.Asm,
            ".cs" => Language.CSharp,
            _ => throw new DrillException("import understands .cs, .py, .c and .s files"),
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
        AnsiConsole.MarkupLine(written == 0 ? "[yellow]no new functions found[/]" : $"{written} snippet{(written == 1 ? "" : "s")} added (trace and recall). Add tests to a file and run [cyan]drill verify[/] to unlock blank mode.");
        return 0;
    }

    private static int Where()
    {
        var root = Workspace.Root();
        AnsiConsole.MarkupLine($"root     {Markup.Escape(root)}");
        AnsiConsole.MarkupLine($"snippets {Markup.Escape(Path.Combine(root, "snippets"))}");
        AnsiConsole.MarkupLine($"database {Markup.Escape(Path.Combine(root, "drill.db"))}");
        AnsiConsole.MarkupLine($"editor   {Markup.Escape(ExternalEditor.Command ?? "built-in (set DRILL_EDITOR to use nano, vim or code --wait)")}");
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
            drill — writing code from a blank file, all the way down: Python, C#, C, ARM64 asm

              drill [lang]            one rep after another; q stops whenever you like
              drill recall [id|lang]  read it, hide it, write it; the tests decide (exact match if none)
              drill blank [id|lang]   the spec and the tests only; write it, Ctrl+D runs the tests
              drill predict [id|lang] the code and each test call; say what it returns before it runs
              drill trace [id|lang]   type over the reference, for syntax that is new to you
              drill down [problem]    one problem down the stack: Python, then C#, then C, then ARM64
              drill families          every problem that exists in several languages, and how far down you are
              drill compile [c-id]    clang's ARM64 for a C snippet, then your own ARM64 for the same tests
              drill bits [topic]      quick-fire hex, twos, shift, mask, endian, pop, arm; never runs out
              drill machine [n|free]  a live ARM64: registers and memory on screen, every line runs as you type it; 30 tasks in order
              drill interview [lang|id] [minutes]  one blank problem, 45 min default, one submission
              drill list [lang] [tag] every snippet with its box, next date and last results
              drill projects          real builds with steps: progress and hours
              drill project <id>      one project's goal, steps and time
              drill project new <id> <title>  a new project file under projects/
              drill work [id] [step]  timed block on the next open step; Enter marks it done
              drill stats             streak, boxes, predict and bits accuracy, weekly blank first-try rate
              drill verify [id...]    check reference code passes its own tests
              drill new <id> <title>  start a new snippet file (id starts with cs-, py-, c- or asm-)
              drill import <file> [tags]  your own functions (.cs .py .c .s) as trace/recall snippets
              drill where             paths, editor and toolchains in use

            Languages: cs (C#, Roslyn in-process), py (python3), c (cc), asm (ARM64, assembled with cc
            and called from a C harness). lang can be given anywhere an id can.

            Every snippet sits in a box. Box 1 (new, or missed last time) is a cold try from the spec and
            the tests; miss it and the reference appears, you read it as long as you need and write it
            again while it is fresh. Boxes 2 to 5 are blank only. Nothing is graded on remembering text:
            the tests decide, and any version that passes is right. A pass moves a snippet up one box and
            it returns after 2, 4, 8 or 16 days; a miss sends it back to box 1 for tomorrow. The session
            serves whatever is ready first, then the weakest, so there is no list to clear. Trace,
            predict, bits and compile never move boxes.
            The number that matters is the blank-mode first-try rate, week over week.
            Recall and blank use a built-in editor: Ctrl+D submits, Esc gives up, Tab indents.
            Set DRILL_EDITOR (nano, vim, "code --wait") only if you really want an external one.
            """);
        return 0;
    }

    private static int Unknown(string command)
    {
        AnsiConsole.MarkupLine($"[red]unknown command '{Markup.Escape(command)}'[/]. Try [cyan]drill help[/].");
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

    /// Every scheduled rep is blank when the snippet has tests: the spec and the tests,
    /// nothing else. Snippets without tests fall back to recall. Trace is never scheduled;
    /// it is there for the asking. Box 1 adds a second act on a miss (see RepAsync).
    private static Mode ModeFor(int box, Snippet snippet)
    {
        if (snippet.Supports(Mode.Blank) && Runner.Available(snippet.Language))
        {
            return Mode.Blank;
        }
        return snippet.Supports(Mode.Recall) || !snippet.Supports(Mode.Trace) ? Mode.Recall : Mode.Trace;
    }

    /// What a rep line calls the mode: a box-1 blank is a cold try.
    private static string Label(int box, Mode mode) => box <= 1 && mode == Mode.Blank ? "cold try" : mode.ToString().ToLowerInvariant();

    /// One scheduled rep. In box 1 you try it cold from the spec and the tests, and a miss
    /// is not the end: the reference appears, you read it for as long as you need, and you
    /// write it again while it is fresh. Nothing here asks you to remember text; the tests
    /// decide, and any version that passes is right.
    private static async Task<AttemptResult> RepAsync(Snippet snippet, int box, Workspace workspace, DateOnly today)
    {
        var mode = ModeFor(box, snippet);
        var cold = box <= 1 && mode == Mode.Blank;
        var result = cold
            ? await RunAsync(mode, snippet, workspace, today, "Write the function below. Ctrl+D runs the tests. Esc shows the reference.")
            : await RunAsync(mode, snippet, workspace, today);
        if (cold && !result.Passed && snippet.Supports(Mode.Recall))
        {
            AnsiConsole.MarkupLine($"[yellow bold]MISS[/]  {Markup.Escape(result.Note)}  ·  [grey]now the reference; read it, then write it again while it's fresh · Enter[/]");
            Console.ReadKey(intercept: true);
            result = await RunAsync(Mode.Recall, snippet, workspace, today);
        }
        return result;
    }

    private static async Task<AttemptResult> RunAsync(Mode mode, Snippet snippet, Workspace workspace, DateOnly today, string? hint = null)
    {
        var result = mode switch
        {
            Mode.Trace => TraceMode.Run(snippet),
            Mode.Recall => await RecallMode.RunAsync(snippet),
            Mode.Predict => PredictMode.Run(snippet),
            _ => await BlankMode.RunAsync(snippet, hint: hint),
        };
        var firstTry = mode switch
        {
            Mode.Trace or Mode.Predict => true,
            Mode.Recall => result.Tries == 1,
            _ => result.Tries == 1 && workspace.Store.BlankTriesToday(snippet.Id, today) == 0,
        };
        workspace.Store.RecordAttempt(new AttemptRow(snippet.Id, mode, DateTime.Now, result.Seconds, result.Accuracy, result.Passed, firstTry));
        return result;
    }

    /// Recall and blank move the snippet between boxes; trace and predict move nothing.
    private static void Report(AttemptResult result, Workspace workspace, Snippet snippet, DateOnly today)
    {
        var verdict = result.Passed ? "[green bold]PASS[/]" : "[red bold]FAIL[/]";
        if (result.Mode is Mode.Trace or Mode.Predict)
        {
            AnsiConsole.MarkupLine($"{verdict}  {Markup.Escape(result.Note)}");
            return;
        }
        var (box, due) = workspace.Store.Advance(snippet.Id, result.Passed, today);
        var days = due.DayNumber - today.DayNumber;
        var next = Label(box, ModeFor(box, snippet));
        AnsiConsole.MarkupLine($"{verdict}  {Markup.Escape(result.Note)}  ·  {next} next, in {days} day{(days == 1 ? "" : "s")}");
    }

    private static void Summary(Workspace workspace, DateOnly today)
    {
        var store = workspace.Store;
        var todayAttempts = store.AttemptsOn(today);
        var rate = Store.BlankFirstTryRate(todayAttempts);
        var cards = store.AllCards().Where(c => workspace.Find(c.Id) is not null).ToList();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]today[/] {todayAttempts.Count} reps · {todayAttempts.Count(a => a.Passed)} passed · blank first-try {(rate is null ? "—" : Pct(rate.Value))}");
        AnsiConsole.MarkupLine($"[bold]streak[/] {store.Streak(today)} days");
        var lastWeek = Enumerable.Range(0, 7).SelectMany(d => store.AttemptsOn(today.AddDays(-d))).ToList();
        var predicted = lastWeek.Where(a => a.Mode == Mode.Predict && a.Accuracy is not null).ToList();
        if (predicted.Count > 0)
        {
            AnsiConsole.MarkupLine($"[bold]predict[/] last 7 days {predicted.Count} snippets · {Pct(predicted.Average(a => a.Accuracy!.Value))} of calls right");
        }
        var machineDone = store.MachineDone().Count;
        if (machineDone > 0)
        {
            AnsiConsole.MarkupLine($"[bold]machine[/] {machineDone}/{MachineTasks.All.Length} tasks done");
        }
        var bits = store.BitsBetween(today.AddDays(-6), today);
        if (bits.Count > 0)
        {
            var asked = bits.Sum(b => b.Asked);
            var correct = bits.Sum(b => b.Correct);
            var weakest = bits.Where(b => b.Asked >= 5).OrderBy(b => b.Accuracy).FirstOrDefault();
            AnsiConsole.MarkupLine($"[bold]bits[/] last 7 days {asked} · {Pct((double)correct / asked)} · {bits.Sum(b => b.MeanSeconds * b.Asked) / asked:0.0}s each{(weakest is null ? "" : $" · weakest {weakest.Kind} {Pct(weakest.Accuracy)}")}");
        }
        var boxTable = new Table().Border(TableBorder.Simple).AddColumn("").AddColumn("box1 cold try").AddColumn("box2 blank").AddColumn("box3").AddColumn("box4").AddColumn("box5");
        foreach (var language in Languages.All)
        {
            var mine = cards.Where(c => workspace.Find(c.Id)!.Language == language).ToList();
            if (mine.Count == 0)
            {
                continue;
            }
            boxTable.AddRow([language.DisplayName(), .. Enumerable.Range(1, Store.MaxBox).Select(b => mine.Count(c => c.Box == b).ToString())]);
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
                var bar = week.Rate is null ? "[grey]no blank reps[/]" : $"[green]{new string('█', (int)Math.Round(week.Rate.Value * 30))}[/][grey]{new string('░', 30 - (int)Math.Round(week.Rate.Value * 30))}[/] {Pct(week.Rate.Value)} ({week.BlankFirstTryPasses}/{week.BlankSnippets})";
                AnsiConsole.MarkupLine($"  {week.WeekStart:dd MMM}  {bar}  [grey]{week.Attempts} reps[/]");
            }
        }
    }

    private static string Pct(double value) => $"{value * 100:0}%";

    private static void RequireTerminal()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            throw new DrillException("drill needs a real terminal for practice modes");
        }
    }
}

public sealed class DrillException(string message) : Exception(message);

/// The repo root (snippets/ and drill.db live there), the loaded snippet bank and the store.
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
        Store = new Store(Path.Combine(root, "drill.db"));
        Store.Sync(Snippets, Today);
    }

    public static Workspace Open()
    {
        var root = Root();
        var workspace = new Workspace(root);
        if (workspace.Snippets.Count == 0)
        {
            workspace.Dispose();
            throw new DrillException($"no snippets found in {Path.Combine(root, "snippets")}");
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
        var configured = Environment.GetEnvironmentVariable("DRILL_HOME");
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
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "drill");
    }

    public Snippet? Find(string id) => Snippets.FirstOrDefault(s => s.Id == id);

    /// Snippets that share a family name, keyed by family, each ordered from the top of
    /// the stack down (Python, C#, C, ARM64).
    public SortedDictionary<string, List<Snippet>> Families()
    {
        var families = new SortedDictionary<string, List<Snippet>>(StringComparer.Ordinal);
        foreach (var group in Snippets.Where(s => s.Family.Length > 0).GroupBy(s => s.Family))
        {
            families[group.Key] = group.OrderBy(s => Array.IndexOf(Languages.Stack, s.Language)).ThenBy(s => s.Id).ToList();
        }
        return families;
    }

    public Snippet Require(string id) => Find(id) ?? throw new DrillException($"no snippet with id {id}. Try: drill list");

    public Project RequireProject(string id) =>
        Projects.FirstOrDefault(p => p.Id == id) ?? throw new DrillException($"no project with id {id}. Try: drill projects");

    public void Dispose() => Store.Dispose();
}
