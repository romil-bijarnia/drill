using System.Diagnostics;
using Spectre.Console;

namespace Drill;

public sealed record ProjectStep(int Number, string Text, bool Done);

/// A real build, not a snippet: a website, an app, a thesis chapter. One Markdown file in
/// `projects/` with a goal and a checklist of steps; `drill work <id>` times a block on the
/// next open step and ticks it off in the file when you say it is done.
public sealed record Project(string Id, string Title, string Status, IReadOnlyList<string> Stack, string Goal, IReadOnlyList<ProjectStep> Steps, string Path)
{
    public int DoneCount => Steps.Count(s => s.Done);

    public ProjectStep? NextStep => Steps.FirstOrDefault(s => !s.Done);

    public bool IsActive => Status is "active" or "";
}

public static class ProjectLoader
{
    public static (List<Project> Projects, List<string> Problems) LoadAll(string directory)
    {
        var projects = new List<Project>();
        var problems = new List<string>();
        if (!Directory.Exists(directory))
        {
            return (projects, problems);
        }
        foreach (var file in Directory.GetFiles(directory, "*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            try
            {
                projects.Add(Parse(File.ReadAllText(file), file));
            }
            catch (FormatException error)
            {
                problems.Add($"bad project: {System.IO.Path.GetFileName(file)} ({error.Message})");
            }
        }
        return (projects, problems);
    }

    public static Project Parse(string text, string path)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        string? id = null, title = null;
        var status = "active";
        var stack = new List<string>();
        var bodyStart = 0;
        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            var end = Array.FindIndex(lines, 1, l => l.Trim() == "---");
            if (end < 0)
            {
                throw new FormatException("missing closing ---");
            }
            foreach (var line in lines[1..end])
            {
                var colon = line.IndexOf(':');
                if (colon <= 0) continue;
                var key = line[..colon].Trim();
                var value = line[(colon + 1)..].Trim().Trim('\'', '"');
                switch (key)
                {
                    case "id": id = value; break;
                    case "title": title = value; break;
                    case "status": status = value.ToLowerInvariant(); break;
                    case "stack":
                        stack = value.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                        break;
                }
            }
            bodyStart = end + 1;
        }
        id ??= System.IO.Path.GetFileNameWithoutExtension(path);
        title ??= id;

        var goal = new List<string>();
        var steps = new List<ProjectStep>();
        foreach (var raw in lines[bodyStart..])
        {
            var line = raw.TrimStart();
            if (line.StartsWith("- [ ] ", StringComparison.Ordinal) || line.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase))
            {
                steps.Add(new ProjectStep(steps.Count + 1, line[6..].Trim(), line[3] is 'x' or 'X'));
            }
            else if (steps.Count == 0 && !line.StartsWith('#') && line.Trim().Length > 0)
            {
                goal.Add(line.Trim());
            }
        }
        if (steps.Count == 0)
        {
            throw new FormatException("no steps; add lines like '- [ ] first thing to build'");
        }
        return new Project(id, title, status, stack, string.Join(" ", goal), steps, path);
    }

    /// Rewrites the checkbox for one step in place, leaving the rest of the file alone.
    public static void MarkDone(Project project, int stepNumber)
    {
        var lines = File.ReadAllText(project.Path).Replace("\r\n", "\n").Split('\n');
        var seen = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("- [ ] ", StringComparison.Ordinal) || trimmed.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase))
            {
                seen++;
                if (seen == stepNumber)
                {
                    var indent = lines[i].Length - trimmed.Length;
                    lines[i] = lines[i][..indent] + "- [x] " + trimmed[6..];
                    break;
                }
            }
        }
        File.WriteAllText(project.Path, string.Join("\n", lines));
    }

    public static string Template(string id, string title) =>
        $"""
        ---
        id: {id}
        title: {title}
        status: active
        stack: []
        ---
        One paragraph: what this is, who it is for, what "done" looks like.

        ## Steps
        - [ ] First small, finishable step
        - [ ] Second step
        - [ ] Ship it somewhere public

        """;
}

/// The timed work block: shows the step, counts up, and logs the minutes.
public static class WorkBlock
{
    public sealed record Outcome(int Step, double Seconds, bool Completed, bool Quit);

    public static Outcome Run(Project project, ProjectStep step, double secondsToday)
    {
        Console.Clear();
        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(project.Id)}[/] [bold]{Markup.Escape(project.Title)}[/]  [grey]{project.DoneCount}/{project.Steps.Count} steps done[/]");
        if (project.Goal.Length > 0)
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(project.Goal)}[/]");
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]step {step.Number}[/]  [bold]{Markup.Escape(step.Text)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Go build it in your real editor. Back here: Enter = step done · p = pause · q = stop for now[/]");
        AnsiConsole.WriteLine();
        var clock = Stopwatch.StartNew();
        var paused = false;
        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                var elapsed = clock.Elapsed;
                var total = TimeSpan.FromSeconds(secondsToday + elapsed.TotalSeconds);
                Console.Write($"\r{Ansi.Dim}{(paused ? "paused " : "working")}{Ansi.Reset}  {elapsed:hh\\:mm\\:ss} on this step   {Ansi.Dim}{total:hh\\:mm\\:ss} on {project.Id} today{Ansi.Reset}{Ansi.ClearLine}");
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        return new Outcome(step.Number, clock.Elapsed.TotalSeconds, true, false);
                    }
                    if (key.KeyChar is 'q' or 'Q' || key.Key == ConsoleKey.Escape)
                    {
                        return new Outcome(step.Number, clock.Elapsed.TotalSeconds, false, true);
                    }
                    if (key.KeyChar is 'p' or 'P')
                    {
                        paused = !paused;
                        if (paused) clock.Stop(); else clock.Start();
                    }
                }
                Thread.Sleep(200);
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.WriteLine();
        }
    }
}
