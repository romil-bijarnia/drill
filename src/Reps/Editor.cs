using System.Diagnostics;
using System.Text;

namespace Reps;

/// A small in-terminal editor for recall and blank mode: enough to write a function
/// without leaving the trainer, and deliberately nothing more. No completion, no
/// snippets, no help. Ctrl+D submits, Esc gives up.
public sealed class TextBox
{
    private readonly List<StringBuilder> _lines = [new StringBuilder()];
    private readonly IReadOnlyList<string> _header;
    private int _row;
    private int _col;
    private int _scroll;

    private readonly Language _language;

    public TextBox(IReadOnlyList<string> header, string initial = "", Language language = Language.CSharp)
    {
        _header = header;
        _language = language;
        if (initial.Length > 0)
        {
            _lines = initial.Replace("\r\n", "\n").Split('\n').Select(l => new StringBuilder(l)).ToList();
            _row = _lines.Count - 1;
            _col = _lines[_row].Length;
        }
    }

    public string Text => string.Join("\n", _lines.Select(l => l.ToString()));

    /// Set when a deadline passed before Ctrl+D; the text at that moment is still returned.
    public bool TimedOut { get; private set; }

    /// Returns the text on Ctrl+D (or when the deadline passes), or null if the user gave
    /// up with Esc.
    public string? Run(DateTime? deadline = null)
    {
        Console.Clear();
        Console.Write(Ansi.BarCursor);
        Console.CursorVisible = true;
        try
        {
            while (true)
            {
                Draw(null, deadline);
                if (deadline is not null)
                {
                    while (!Console.KeyAvailable)
                    {
                        if (DateTime.UtcNow >= deadline.Value)
                        {
                            TimedOut = true;
                            return Text;
                        }
                        Thread.Sleep(100);
                        Draw(null, deadline);
                    }
                }
                var key = Console.ReadKey(intercept: true);
                var control = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                if (key.Key == ConsoleKey.Escape)
                {
                    Draw("give up on this rep? y / n", deadline);
                    var answer = Console.ReadKey(intercept: true);
                    if (answer.KeyChar is 'y' or 'Y')
                    {
                        return null;
                    }
                    continue;
                }
                if ((control && key.Key == ConsoleKey.D) || key.Key == ConsoleKey.F5 || key.KeyChar == '')
                {
                    return Text;
                }
                Handle(key, control);
            }
        }
        finally
        {
            Console.Write(Ansi.Reset);
            Console.Write(Ansi.DefaultCursor);
            Console.Write("\e[J");
        }
    }

    private void Handle(ConsoleKeyInfo key, bool control)
    {
        var line = _lines[_row];
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_col > 0) _col--;
                else if (_row > 0) { _row--; _col = _lines[_row].Length; }
                return;
            case ConsoleKey.RightArrow:
                if (_col < line.Length) _col++;
                else if (_row < _lines.Count - 1) { _row++; _col = 0; }
                return;
            case ConsoleKey.UpArrow:
                if (_row > 0) { _row--; _col = Math.Min(_col, _lines[_row].Length); }
                return;
            case ConsoleKey.DownArrow:
                if (_row < _lines.Count - 1) { _row++; _col = Math.Min(_col, _lines[_row].Length); }
                return;
            case ConsoleKey.Home:
                _col = 0;
                return;
            case ConsoleKey.End:
                _col = line.Length;
                return;
            case ConsoleKey.Enter:
                {
                    var indent = Indentation(line.ToString());
                    var trimmed = line.ToString().TrimEnd();
                    if (_language == Language.Python ? trimmed.EndsWith(':') : trimmed.EndsWith('{'))
                    {
                        indent += 4;
                    }
                    var tail = line.ToString(_col, line.Length - _col);
                    line.Length = _col;
                    _lines.Insert(_row + 1, new StringBuilder(new string(' ', indent) + tail.TrimStart()));
                    _row++;
                    _col = indent;
                    return;
                }
            case ConsoleKey.Backspace:
                if (_col > 0)
                {
                    var remove = 1;
                    if (_col % 4 == 0 && _col >= 4 && line.ToString(0, _col).Trim().Length == 0)
                    {
                        remove = 4; // a soft tab goes in one keystroke
                    }
                    line.Remove(_col - remove, remove);
                    _col -= remove;
                }
                else if (_row > 0)
                {
                    var previous = _lines[_row - 1];
                    _col = previous.Length;
                    previous.Append(line);
                    _lines.RemoveAt(_row);
                    _row--;
                }
                return;
            case ConsoleKey.Delete:
                if (_col < line.Length) line.Remove(_col, 1);
                else if (_row < _lines.Count - 1) { line.Append(_lines[_row + 1]); _lines.RemoveAt(_row + 1); }
                return;
            case ConsoleKey.Tab:
                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    Dedent(line);
                }
                else
                {
                    line.Insert(_col, "    ");
                    _col += 4;
                }
                return;
        }
        if (control)
        {
            switch (key.Key)
            {
                case ConsoleKey.A: _col = 0; return;
                case ConsoleKey.E: _col = line.Length; return;
                case ConsoleKey.K:
                    if (_lines.Count > 1) { _lines.RemoveAt(_row); _row = Math.Min(_row, _lines.Count - 1); }
                    else line.Clear();
                    _col = Math.Min(_col, _lines[_row].Length);
                    return;
                default: return;
            }
        }
        var c = key.KeyChar;
        if (c == '\0' || char.IsControl(c))
        {
            return;
        }
        if (c == '}' && _language != Language.Python && line.ToString().Trim().Length == 0)
        {
            Dedent(line); // a closing brace on its own line steps back out
        }
        line.Insert(_col, c);
        _col++;
    }

    private void Dedent(StringBuilder line)
    {
        var spaces = Math.Min(4, Indentation(line.ToString()));
        if (spaces == 0)
        {
            return;
        }
        line.Remove(0, spaces);
        _col = Math.Max(0, _col - spaces);
    }

    private static int Indentation(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ') count++;
        return count;
    }

    private void Draw(string? prompt, DateTime? deadline = null)
    {
        var width = Math.Max(40, SafeWidth());
        var height = Math.Max(12, SafeHeight());
        var editorTop = _header.Count + 1;
        var visible = Math.Max(3, height - editorTop - 2);
        if (_row < _scroll) _scroll = _row;
        if (_row >= _scroll + visible) _scroll = _row - visible + 1;

        var output = new StringBuilder("\e[H");
        foreach (var line in _header)
        {
            output.Append(Fit(line, width - 1)).Append(Ansi.ClearLine).Append('\n');
        }
        output.Append(Ansi.Dim).Append(new string('─', width - 1)).Append(Ansi.Reset).Append(Ansi.ClearLine).Append('\n');
        for (var i = 0; i < visible; i++)
        {
            var index = _scroll + i;
            if (index < _lines.Count)
            {
                output.Append(Ansi.Dim).Append((index + 1).ToString().PadLeft(3)).Append(' ').Append(Ansi.Reset)
                    .Append(Fit(_lines[index].ToString(), width - 5));
            }
            output.Append(Ansi.ClearLine).Append('\n');
        }
        var remaining = deadline is null ? "" : $"{(deadline.Value - DateTime.UtcNow):mm\\:ss} left · ";
        var status = prompt ?? $"{remaining}Ctrl+D done · Esc give up · Tab indents · ln {_row + 1}, col {_col + 1}";
        output.Append(prompt is null ? Ansi.Dim : "\e[33m").Append(Fit(status, width - 1)).Append(Ansi.Reset).Append(Ansi.ClearLine);
        output.Append("\e[J");
        var cursorRow = editorTop + (_row - _scroll) + 1;
        var cursorCol = Math.Min(4 + _col, width - 1) + 1;
        output.Append("\e[").Append(cursorRow).Append(';').Append(cursorCol).Append('H');
        Console.Write(output.ToString());
    }

    private static string Fit(string text, int width) => text.Length <= width ? text : text[..Math.Max(0, width - 1)] + "…";

    private static int SafeWidth()
    {
        try { return Console.WindowWidth; } catch { return 120; }
    }

    private static int SafeHeight()
    {
        try { return Console.WindowHeight; } catch { return 40; }
    }
}

/// Optional hand-off to an external editor when REPS_EDITOR is set (nano, vim, or
/// `code --wait`). The built-in TextBox is the default because it has no autocomplete.
public static class ExternalEditor
{
    public static string? Command => Environment.GetEnvironmentVariable("REPS_EDITOR") is { Length: > 0 } value ? value : null;

    public static string? Edit(string initial, string extension, int cursorLine)
    {
        var command = Command;
        if (command is null)
        {
            return null;
        }
        var path = Path.Combine(Path.GetTempPath(), $"reps-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, initial);
        try
        {
            var parts = Split(command);
            var info = new ProcessStartInfo(parts[0]) { UseShellExecute = false };
            foreach (var argument in parts.Skip(1))
            {
                info.ArgumentList.Add(argument);
            }
            if (Path.GetFileName(parts[0]) is "nano" or "vim" or "vi" or "nvim")
            {
                info.ArgumentList.Add($"+{Math.Max(1, cursorLine)}");
            }
            info.ArgumentList.Add(path);
            using var process = Process.Start(info);
            process?.WaitForExit();
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        finally
        {
            try { File.Delete(path); } catch { /* scratch file */ }
        }
    }

    private static List<string> Split(string command)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var quote = '\0';
        foreach (var c in command)
        {
            if (quote != '\0')
            {
                if (c == quote) quote = '\0'; else current.Append(c);
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0) { parts.Add(current.ToString()); current.Clear(); }
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0) parts.Add(current.ToString());
        return parts;
    }
}
