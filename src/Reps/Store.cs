using Microsoft.Data.Sqlite;

namespace Reps;

public sealed record Card(string Id, int Box, DateOnly NextDue);

public sealed record AttemptRow(string SnippetId, Mode Mode, DateTime StartedAt, double Seconds, double? Accuracy, bool Passed, bool FirstTry);

public sealed record WeekStat(DateOnly WeekStart, int BlankSnippets, int BlankFirstTryPasses, int Attempts)
{
    public double? Rate => BlankSnippets == 0 ? null : (double)BlankFirstTryPasses / BlankSnippets;
}

/// SQLite storage: which box each snippet sits in, every attempt, and one row per day.
public sealed class Store : IDisposable
{
    public const int MaxBox = 5;

    private readonly SqliteConnection _connection;

    public Store(string path)
    {
        _connection = new SqliteConnection($"Data Source={path}");
        _connection.Open();
        Migrate();
    }

    public static Store InMemory() => new(":memory:");

    private void Migrate()
    {
        Execute("""
            CREATE TABLE IF NOT EXISTS snippets (
                id        TEXT PRIMARY KEY,
                title     TEXT NOT NULL,
                tags      TEXT NOT NULL,
                box       INTEGER NOT NULL DEFAULT 1,
                next_due  TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS attempts (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                snippet_id TEXT NOT NULL REFERENCES snippets(id),
                mode       TEXT NOT NULL,
                started_at TEXT NOT NULL,
                seconds    REAL NOT NULL,
                accuracy   REAL,
                passed     INTEGER NOT NULL,
                first_try  INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS sessions (
                date                  TEXT PRIMARY KEY,
                attempts              INTEGER NOT NULL,
                blank_first_try_rate  REAL
            );
            CREATE TABLE IF NOT EXISTS work (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                project_id TEXT NOT NULL,
                step       INTEGER NOT NULL,
                started_at TEXT NOT NULL,
                seconds    REAL NOT NULL,
                completed  INTEGER NOT NULL
            );
            """);
    }

    /// Registers snippets that are new on disk (box 1, due today) and refreshes titles.
    public void Sync(IEnumerable<Snippet> snippets, DateOnly today)
    {
        using var transaction = _connection.BeginTransaction();
        foreach (var snippet in snippets)
        {
            Execute(
                "INSERT INTO snippets (id, title, tags, box, next_due) VALUES ($id, $title, $tags, 1, $due) " +
                "ON CONFLICT(id) DO UPDATE SET title = excluded.title, tags = excluded.tags",
                ("$id", snippet.Id), ("$title", snippet.Title), ("$tags", string.Join(",", snippet.Tags)), ("$due", today.ToString("yyyy-MM-dd")));
        }
        transaction.Commit();
    }

    public Card? GetCard(string id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT id, box, next_due FROM snippets WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadCard(reader) : null;
    }

    public List<Card> AllCards()
    {
        var cards = new List<Card>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT id, box, next_due FROM snippets ORDER BY id";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cards.Add(ReadCard(reader));
        }
        return cards;
    }

    public List<Card> Due(DateOnly today) =>
        AllCards().Where(c => c.NextDue <= today).OrderBy(c => c.NextDue).ThenBy(c => c.Box).ThenBy(c => c.Id).ToList();

    private static Card ReadCard(SqliteDataReader reader) =>
        new(reader.GetString(0), reader.GetInt32(1), DateOnly.Parse(reader.GetString(2)));

    /// Leitner: pass moves the snippet up one box (max 5), fail drops it to box 1.
    /// A snippet in box n comes back after 2^(n-1) days.
    public static (int Box, DateOnly NextDue) Schedule(int currentBox, bool passed, DateOnly today)
    {
        var box = passed ? Math.Min(MaxBox, currentBox + 1) : 1;
        return (box, today.AddDays(1 << (box - 1)));
    }

    public (int Box, DateOnly NextDue) Advance(string id, bool passed, DateOnly today)
    {
        var card = GetCard(id) ?? throw new InvalidOperationException($"unknown snippet {id}");
        var (box, due) = Schedule(card.Box, passed, today);
        Execute("UPDATE snippets SET box = $box, next_due = $due WHERE id = $id",
            ("$box", box), ("$due", due.ToString("yyyy-MM-dd")), ("$id", id));
        return (box, due);
    }

    public void RecordAttempt(AttemptRow attempt)
    {
        Execute(
            "INSERT INTO attempts (snippet_id, mode, started_at, seconds, accuracy, passed, first_try) " +
            "VALUES ($id, $mode, $started, $seconds, $accuracy, $passed, $first)",
            ("$id", attempt.SnippetId), ("$mode", attempt.Mode.ToString().ToLowerInvariant()),
            ("$started", attempt.StartedAt.ToString("o")), ("$seconds", attempt.Seconds),
            ("$accuracy", (object?)attempt.Accuracy ?? DBNull.Value), ("$passed", attempt.Passed ? 1 : 0),
            ("$first", attempt.FirstTry ? 1 : 0));
    }

    public int BlankTriesToday(string id, DateOnly today)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM attempts WHERE snippet_id = $id AND mode = 'blank' AND substr(started_at, 1, 10) = $day";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$day", today.ToString("yyyy-MM-dd"));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<AttemptRow> RecentAttempts(string id, int limit)
    {
        var rows = new List<AttemptRow>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT snippet_id, mode, started_at, seconds, accuracy, passed, first_try FROM attempts WHERE snippet_id = $id ORDER BY id DESC LIMIT $limit";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(ReadAttempt(reader));
        }
        return rows;
    }

    public List<AttemptRow> AttemptsOn(DateOnly day)
    {
        var rows = new List<AttemptRow>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT snippet_id, mode, started_at, seconds, accuracy, passed, first_try FROM attempts WHERE substr(started_at, 1, 10) = $day ORDER BY id";
        command.Parameters.AddWithValue("$day", day.ToString("yyyy-MM-dd"));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(ReadAttempt(reader));
        }
        return rows;
    }

    private static AttemptRow ReadAttempt(SqliteDataReader reader) => new(
        reader.GetString(0),
        Enum.Parse<Mode>(reader.GetString(1), ignoreCase: true),
        DateTime.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
        reader.GetDouble(3),
        reader.IsDBNull(4) ? null : reader.GetDouble(4),
        reader.GetInt32(5) == 1,
        reader.GetInt32(6) == 1);

    /// The number that matters: of the snippets attempted in blank mode today, how many
    /// passed on the first try.
    public static double? BlankFirstTryRate(IEnumerable<AttemptRow> attempts)
    {
        var bySnippet = attempts.Where(a => a.Mode == Mode.Blank).GroupBy(a => a.SnippetId).ToList();
        if (bySnippet.Count == 0)
        {
            return null;
        }
        var firstTryPasses = bySnippet.Count(g => g.OrderBy(a => a.StartedAt).First() is { Passed: true, FirstTry: true });
        return (double)firstTryPasses / bySnippet.Count;
    }

    public void UpsertSession(DateOnly day)
    {
        var attempts = AttemptsOn(day);
        var rate = BlankFirstTryRate(attempts);
        Execute(
            "INSERT INTO sessions (date, attempts, blank_first_try_rate) VALUES ($date, $count, $rate) " +
            "ON CONFLICT(date) DO UPDATE SET attempts = excluded.attempts, blank_first_try_rate = excluded.blank_first_try_rate",
            ("$date", day.ToString("yyyy-MM-dd")), ("$count", attempts.Count), ("$rate", (object?)rate ?? DBNull.Value));
    }

    public List<WeekStat> Weekly(int weeks, DateOnly today)
    {
        var start = today.AddDays(-(int)today.DayOfWeek + 1); // Monday of this week
        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            start = today.AddDays(-6);
        }
        var stats = new List<WeekStat>();
        for (var w = weeks - 1; w >= 0; w--)
        {
            var weekStart = start.AddDays(-7 * w);
            var weekEnd = weekStart.AddDays(6);
            var attempts = new List<AttemptRow>();
            for (var d = weekStart; d <= weekEnd; d = d.AddDays(1))
            {
                attempts.AddRange(AttemptsOn(d));
            }
            var blank = attempts.Where(a => a.Mode == Mode.Blank).GroupBy(a => (a.SnippetId, Day: DateOnly.FromDateTime(a.StartedAt))).ToList();
            var passes = blank.Count(g => g.OrderBy(a => a.StartedAt).First() is { Passed: true, FirstTry: true });
            stats.Add(new WeekStat(weekStart, blank.Count, passes, attempts.Count));
        }
        return stats;
    }

    public void LogWork(string projectId, int step, DateTime startedAt, double seconds, bool completed)
    {
        Execute(
            "INSERT INTO work (project_id, step, started_at, seconds, completed) VALUES ($id, $step, $started, $seconds, $done)",
            ("$id", projectId), ("$step", step), ("$started", startedAt.ToString("o")), ("$seconds", seconds), ("$done", completed ? 1 : 0));
    }

    /// Seconds of project work between two days inclusive, optionally for one project.
    public double WorkSeconds(DateOnly from, DateOnly to, string? projectId = null)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(seconds), 0) FROM work WHERE substr(started_at, 1, 10) BETWEEN $from AND $to"
            + (projectId is null ? "" : " AND project_id = $id");
        command.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        if (projectId is not null)
        {
            command.Parameters.AddWithValue("$id", projectId);
        }
        return Convert.ToDouble(command.ExecuteScalar());
    }

    public int StepsCompleted(DateOnly from, DateOnly to)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM work WHERE completed = 1 AND substr(started_at, 1, 10) BETWEEN $from AND $to";
        command.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int SessionDays()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sessions";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int Streak(DateOnly today)
    {
        var days = new HashSet<DateOnly>();
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT DISTINCT substr(started_at, 1, 10) FROM attempts UNION SELECT DISTINCT substr(started_at, 1, 10) FROM work";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                days.Add(DateOnly.Parse(reader.GetString(0)));
            }
        }
        var streak = 0;
        var day = days.Contains(today) ? today : today.AddDays(-1);
        while (days.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    private void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
