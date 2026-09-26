using System.Diagnostics;
using System.Text;
using Spectre.Console;

namespace Drill;

public sealed record MachineTask(int Number, string Goal, Action<Machine> Setup, Func<Machine, bool> Check, string Hint);

/// The live machine: registers, flags and memory on screen; every line you type runs the
/// moment you press Enter and whatever changed lights up. Backward branches replay the
/// tape so you watch a loop go round. Tasks give it an aim: a state to reach, in order.
public static class MachineMode
{
    private const int Delay = 70;

    public static int Run(Store store, string? arg)
    {
        var machine = new Machine();
        var done = store.MachineDone();
        MachineTask? task = arg switch
        {
            "free" => null,
            null => MachineTasks.All.FirstOrDefault(t => !done.Contains(t.Number.ToString())),
            _ => int.TryParse(arg, out var n) ? MachineTasks.All.FirstOrDefault(t => t.Number == n) ?? throw new DrillException($"tasks go from 1 to {MachineTasks.All.Length}") : throw new DrillException("drill machine [free | task number]"),
        };
        var allDone = arg is null && task is null;
        Setup(machine, task);
        var clock = Stopwatch.StartNew();
        var lines = 0;
        string? message = allDone ? $"all {MachineTasks.All.Length} tasks done. Free play: type instructions, or drill machine <n> to redo one." : null;
        Console.Clear();
        while (true)
        {
            Draw(machine, task, message, null, done.Count);
            var input = Console.ReadLine();
            if (input is null)
            {
                break;
            }
            var text = input.Trim();
            if (text.Length == 0)
            {
                continue;
            }
            message = null;
            var word = text.Split(' ', 2)[0].ToLowerInvariant();
            var rest = text.Contains(' ') ? text.Split(' ', 2)[1].Trim() : "";
            try
            {
                switch (word)
                {
                    case "q" or "quit" or "exit":
                        Console.Clear();
                        return 0;
                    case "help" or "?":
                        Help();
                        continue;
                    case "reset":
                        machine.Tape.Clear();
                        Setup(machine, task);
                        lines = 0;
                        continue;
                    case "undo":
                        if (machine.Tape.Count > 0)
                        {
                            machine.Tape.RemoveAt(machine.Tape.Count - 1);
                            Replay(machine, task, null);
                        }
                        continue;
                    case "list":
                        message = machine.Tape.Count == 0 ? "the tape is empty" : string.Join(" · ", machine.Tape.Select((l, i) => $"{i + 1} {l.Text}"));
                        continue;
                    case "save":
                        {
                            var path = rest.Length > 0 ? rest : "machine.s";
                            File.WriteAllText(path, string.Join("\n", machine.Tape.Where(l => l.Directive is null).Select(l => l.Label is not null && l.Mnemonic is null ? l.Label + ":" : l.Text)) + "\n");
                            message = $"saved {machine.Tape.Count(l => l.Directive is null)} lines to {path}";
                            continue;
                        }
                    case "load":
                        {
                            if (!File.Exists(rest)) throw new MachineException($"{rest} does not exist");
                            foreach (var raw in File.ReadAllLines(rest))
                            {
                                var trimmed = raw.Trim();
                                if (trimmed.Length == 0 || trimmed.StartsWith('.') || trimmed.StartsWith("//") || trimmed.StartsWith(';')) continue;
                                machine.Tape.Add(Machine.Parse(trimmed));
                                lines++;
                            }
                            Replay(machine, task, null);
                            continue;
                        }
                    case "show":
                        machine.Focus = Machine.ParseImm(rest) & ~0xFL;
                        continue;
                    case "hint":
                        message = task is null ? "free play has no task; drill machine to get one" : "hint: " + task.Hint;
                        continue;
                    case "free":
                        task = null;
                        machine.Tape.Clear();
                        Setup(machine, null);
                        continue;
                    case "skip" or "next":
                        task = task is null ? MachineTasks.All.FirstOrDefault(t => !done.Contains(t.Number.ToString())) : MachineTasks.All.FirstOrDefault(t => t.Number > task.Number);
                        machine.Tape.Clear();
                        Setup(machine, task);
                        clock.Restart();
                        lines = 0;
                        message = task is null ? "no more tasks; free play" : null;
                        continue;
                }
                var line = Machine.Parse(text);
                machine.Tape.Add(line);
                lines++;
                try
                {
                    ExecuteNew(machine, task, done.Count);
                }
                catch (MachineException)
                {
                    machine.Tape.RemoveAt(machine.Tape.Count - 1);
                    lines--;
                    Replay(machine, task, null);
                    throw;
                }
                if (machine.Waiting is not null)
                {
                    message = $"waiting for the label {machine.Waiting}: keep typing, it runs on when the label appears";
                }
                if (task is not null && machine.Waiting is null && task.Check(machine))
                {
                    clock.Stop();
                    store.MachineComplete(task.Number.ToString(), DateTime.Now, clock.Elapsed.TotalSeconds, lines);
                    done.Add(task.Number.ToString());
                    Draw(machine, task, $"✓ task {task.Number} done · {lines} line{(lines == 1 ? "" : "s")} · {clock.Elapsed.TotalSeconds:0}s · Enter for the next one", null, done.Count);
                    Console.ReadKey(intercept: true);
                    task = MachineTasks.All.FirstOrDefault(t => t.Number > task.Number && !done.Contains(t.Number.ToString()))
                        ?? MachineTasks.All.FirstOrDefault(t => !done.Contains(t.Number.ToString()));
                    machine.Tape.Clear();
                    Setup(machine, task);
                    clock.Restart();
                    lines = 0;
                    message = task is null ? $"all {MachineTasks.All.Length} tasks done. Free play from here." : null;
                }
            }
            catch (MachineException error)
            {
                message = error.Message;
            }
        }
        Console.Clear();
        return 0;
    }

    private static void Setup(Machine machine, MachineTask? task)
    {
        machine.Reset();
        if (task is null)
        {
            machine.Directive("mem 0x1000 = \"hello\"");
        }
        else
        {
            task.Setup(machine);
        }
        machine.ClearTouched();
    }

    /// Runs the line just added. If the tape was waiting on a label, everything replays
    /// from the start and the animation begins where it had stopped.
    private static void ExecuteNew(Machine machine, MachineTask? task, int doneCount)
    {
        var index = machine.Tape.Count - 1;
        if (machine.Waiting is not null)
        {
            var from = machine.WaitingAt;
            Replay(machine, task, from, doneCount);
            return;
        }
        machine.ClearTouched();
        var steps = 0;
        machine.Run(index, pc =>
        {
            steps++;
            if (steps > 1 && steps < 400)
            {
                Draw(machine, task, null, pc, doneCount);
                Thread.Sleep(Delay);
                machine.ClearTouched();
            }
        });
    }

    private static void Replay(Machine machine, MachineTask? task, int? animateFrom, int doneCount = 0)
    {
        var tape = machine.Tape.ToList();
        machine.Tape.Clear();
        Setup(machine, task);
        machine.Tape.AddRange(tape);
        if (machine.Tape.Count == 0)
        {
            return;
        }
        var animating = false;
        var shown = 0;
        machine.Run(0, pc =>
        {
            if (animateFrom is not null && pc == animateFrom) animating = true;
            if (animating && shown++ < 400)
            {
                Draw(machine, task, null, pc, doneCount);
                Thread.Sleep(Delay);
                machine.ClearTouched();
            }
        });
    }

    // MARK: drawing

    private static readonly int[] Rows = [0, 3, 6, 9, 12, 19];

    private static void Draw(Machine m, MachineTask? task, string? message, int? current, int doneCount)
    {
        var width = Ui.Width();
        var height = SafeHeight();
        var o = new StringBuilder("\e[H");
        void Line(string text) => o.Append(text).Append(Ansi.ClearLine).Append('\n');

        Line(task is null
            ? $"\e[1mmachine\e[0m · free play · {Ansi.Dim}q quits · help · {doneCount}/{MachineTasks.All.Length} tasks done{Ansi.Reset}"
            : $"\e[1mmachine\e[0m · task {task.Number} of {MachineTasks.All.Length} · {Ansi.Dim}hint · skip · reset · undo · q{Ansi.Reset}");
        Line(task is null ? $"{Ansi.Dim}type an instruction and watch; x1 = 5 sets a register, mem 0x1000 = \"hi\" sets bytes{Ansi.Reset}" : "\e[36m" + Fit(task.Goal, width - 1) + Ansi.Reset);

        foreach (var start in Rows)
        {
            var row = new StringBuilder();
            for (var r = start; r < start + 3 && r < 31; r++)
            {
                if (start == 19 && r > 21) break;
                row.Append(Register(m, r));
            }
            Line(row.ToString());
        }
        Line(Register(m, 29) + Register(m, 30) + $"{"sp",-3} " + Hex(m.Sp, m.SpTouched) + "  ");
        var flags = $"{Flag('N', m.N)} {Flag('Z', m.Z)} {Flag('C', m.C)} {Flag('V', m.V)}";
        Line($"{Ansi.Dim}flags{Ansi.Reset} {(m.FlagsTouched ? "\e[33m" : "")}{flags}{Ansi.Reset}   {Ansi.Dim}steps {m.Steps}{Ansi.Reset}");

        for (var row = 0; row < 4; row++)
        {
            var address = m.Focus + row * 16;
            var hex = new StringBuilder();
            var ascii = new StringBuilder();
            for (var i = 0; i < 16; i++)
            {
                var a = address + i;
                var b = a < Machine.MemorySize ? m.Memory[a] : (byte)0;
                var touched = m.TouchedBytes.Contains(a);
                hex.Append(touched ? "\e[33m" : b == 0 ? Ansi.Dim : "").Append(b.ToString("x2")).Append(Ansi.Reset).Append(' ');
                ascii.Append(b is >= 32 and < 127 ? (char)b : '.');
            }
            Line($"{Ansi.Dim}{address:x5}{Ansi.Reset}  {hex}{Ansi.Dim}|{ascii}|{Ansi.Reset}");
        }

        Line($"{Ansi.Dim}{new string('─', Math.Min(width - 1, 78))}{Ansi.Reset}");
        var tapeRows = Math.Max(3, height - 17);
        var first = Math.Max(0, m.Tape.Count - tapeRows);
        for (var i = first; i < m.Tape.Count; i++)
        {
            var mark = current == i ? "\e[33m▶" : " ";
            var text = m.Tape[i].Text;
            if (m.Tape[i].Label is not null && m.Tape[i].Mnemonic is null) text = m.Tape[i].Label + ":";
            Line($"{mark} {Ansi.Dim}{i + 1,3}{Ansi.Reset}  {Fit(text, width - 8)}{Ansi.Reset}");
        }
        for (var i = m.Tape.Count - first; i < tapeRows; i++)
        {
            Line("");
        }
        if (message is not null)
        {
            var colour = message.StartsWith('✓') ? "\e[32m" : message.StartsWith("waiting") || message.StartsWith("hint") || message.StartsWith("saved") || message.StartsWith("all") ? "\e[33m" : "\e[31m";
            Line(colour + Fit(message, width - 1) + Ansi.Reset);
        }
        else
        {
            Line("");
        }
        o.Append("> ").Append(Ansi.ClearLine).Append("\e[J");
        Console.Write(o.ToString());
    }

    private static string Register(Machine m, int r)
    {
        var name = r == 29 ? "fp" : r == 30 ? "lr" : $"x{r}";
        return $"{name,-3} " + Hex(m.Regs[r], m.TouchedRegs.Contains(r)) + "  ";
    }

    private static string Hex(long value, bool touched)
    {
        var text = value.ToString("x16");
        var significant = text.TrimStart('0');
        var zeros = text.Length - significant.Length;
        if (touched)
        {
            return "\e[33m" + text + Ansi.Reset;
        }
        if (significant.Length == 0)
        {
            return Ansi.Dim + text + Ansi.Reset;
        }
        return Ansi.Dim + text[..zeros] + Ansi.Reset + significant;
    }

    private static string Flag(char name, bool on) => $"{name}{(on ? 1 : 0)}";

    private static string Fit(string text, int width) => text.Length <= width ? text : text[..Math.Max(0, width - 1)] + "…";

    private static int SafeHeight()
    {
        try { return Math.Max(24, Console.WindowHeight); } catch { return 40; }
    }

    private static void Help()
    {
        Console.Clear();
        AnsiConsole.WriteLine("""
            machine — a live ARM64. Type one instruction and it runs; what changed turns yellow.

              add x0, x1, x2        registers x0–x30 (w0–w30 for 32 bits), sp, lr, fp, xzr
              mov x1, #0x1f         immediates: #5 #-8 #0x1f #0b101 #'a'
              ldr x0, [x1, #8]      memory: [x1]  [x1, #8]  [x1, #8]!  [x1], #8  [x1, x2, lsl #3]
              loop:                 a label; b loop / cbz x1, done / b.ne loop jump to it,
                                    backwards replays the tape so you watch the loop run,
                                    forwards waits until you type the label
              bl double / ret       calls: bl stores the return point in lr; ret goes back
              x1 = 5                set a register directly    mem 0x1000 = "hello"  set bytes
              mem 0x1000 = 48 65    set bytes in hex           show 0x1020           move the memory window

              undo    take back the last line       reset   empty the tape
              list    the whole tape                save f.s / load f.s
              hint    one hint for the task         skip    next task     free   no task
              q       quit

            Instructions this machine knows:
            """ + Machine.Instructions + "\n\nData lives at 0x1000; the stack starts at 0xf000 and grows down. Press any key.");
        Console.ReadKey(intercept: true);
        Console.Clear();
    }
}

public static class MachineTasks
{
    private static long L(Machine m, long address) => BitConverter.ToInt64(m.Memory, (int)address);

    private static MachineTask T(int n, string goal, Action<Machine> setup, Func<Machine, bool> check, string hint) => new(n, goal, setup, check, hint);

    public static readonly MachineTask[] All =
    [
        T(1, "x0 holds 5. Put 12 into x1.", m => m.Regs[0] = 5, m => m.Regs[1] == 12, "mov x1, #12"),
        T(2, "x0 = 5, x1 = 7. Put their sum in x2.", m => { m.Regs[0] = 5; m.Regs[1] = 7; }, m => m.Regs[2] == 12, "add x2, x0, x1"),
        T(3, "x0 = 9. Put x0 minus 4 in x1, and x1 minus x0 in x2. Look at x2 in hex: that is a negative number.", m => m.Regs[0] = 9, m => m.Regs[1] == 5 && m.Regs[2] == -4, "sub x1, x0, #4 then sub x2, x1, x0"),
        T(4, "x0 = 6. Double it in place with a shift, not an add.", m => m.Regs[0] = 6, m => m.Regs[0] == 12, "lsl x0, x0, #1"),
        T(5, "x0 = 0x2f. Put its low nibble in x1 and its high nibble in x2.", m => m.Regs[0] = 0x2f, m => m.Regs[1] == 0xf && m.Regs[2] == 2, "and x1, x0, #0xf then lsr x2, x0, #4"),
        T(6, "x0 = 100, x1 = 7. Quotient into x2, remainder into x3.", m => { m.Regs[0] = 100; m.Regs[1] = 7; }, m => m.Regs[2] == 14 && m.Regs[3] == 2, "udiv x2, x0, x1 then msub x3, x2, x1, x0"),
        T(7, "x0 = 25. Put -25 in x1 and look at it: that is two's complement.", m => m.Regs[0] = 25, m => m.Regs[1] == -25, "neg x1, x0"),
        T(8, "x0 = 0xb6. Set bit 0 and clear bit 7; result in x1.", m => m.Regs[0] = 0xb6, m => m.Regs[1] == 0x37, "orr x1, x0, #1 then and x1, x1, #0x7f (or bic with #0x80)"),
        T(9, "x0 = 3, x1 = 4. Swap them using only eor, no third register.", m => { m.Regs[0] = 3; m.Regs[1] = 4; }, m => m.Regs[0] == 4 && m.Regs[1] == 3, "eor x0, x0, x1 · eor x1, x0, x1 · eor x0, x0, x1"),
        T(10, "w1 = 0xffffffff. Add 1 to it as a 32-bit value into w2, then look at x2.", m => m.Regs[1] = 0xffffffffL, m => m.Regs[2] == 0 && m.Tape.Any(l => l.Mnemonic == "add"), "add w2, w1, #1: the 33rd bit falls off"),
        T(11, "x0 = 3, x1 = 8. x2 = 1 if x0 < x1, else 0. Compare, then set.", m => { m.Regs[0] = 3; m.Regs[1] = 8; }, m => m.Regs[2] == 1 && m.FlagsEverSet, "cmp x0, x1 then cset x2, lt"),
        T(12, "x0 = -1. x1 = 1 if x0 is negative, else 0. Watch the N flag.", m => m.Regs[0] = -1, m => m.Regs[1] == 1 && m.FlagsEverSet, "cmp x0, #0 then cset x1, mi (or lt)"),
        T(13, "x0 = 5, x1 = 9. The larger into x2, without a branch.", m => { m.Regs[0] = 5; m.Regs[1] = 9; }, m => m.Regs[2] == 9 && m.FlagsEverSet, "cmp x0, x1 then csel x2, x0, x1, gt"),
        T(14, "x0 = -7. Its absolute value into x1, without a branch.", m => m.Regs[0] = -7, m => m.Regs[1] == 7, "cmp x0, #0 then cneg x1, x0, mi"),
        T(15, "Store the byte 0x41 at address 0x1000 and watch it appear.", m => { }, m => m.Memory[0x1000] == 0x41, "mov x1, #0x1000 · mov w0, #0x41 · strb w0, [x1]"),
        T(16, "\"hello\" sits at 0x1000 and x1 points at it. Load the second character into w0.", m => { m.Directive("mem 0x1000 = \"hello\""); m.Regs[1] = 0x1000; }, m => m.Regs[0] == 'e', "ldrb w0, [x1, #1]"),
        T(17, "x0 = 0x1122334455667788, x1 = 0x1000. Store x0 at 0x1010 and see which byte lands first.", m => { m.Regs[0] = 0x1122334455667788; m.Regs[1] = 0x1000; }, m => L(m, 0x1010) == 0x1122334455667788, "str x0, [x1, #16]: little-endian puts 88 first"),
        T(18, "Bytes 78 56 34 12 sit at 0x1020 and x1 = 0x1000. Load them as one 32-bit value into w0.", m => { m.Directive("mem 0x1020 = 78 56 34 12"); m.Regs[1] = 0x1000; }, m => m.Regs[0] == 0x12345678, "ldr w0, [x1, #32]"),
        T(19, "Four longs 1, 2, 3, 4 sit at 0x1000 and x1 points there. Load the third into x0 with an immediate offset.", Longs1234, m => m.Regs[0] == 3, "ldr x0, [x1, #16]: each long is 8 bytes"),
        T(20, "Same four longs, x1 points there, x2 = 1. Load element number x2 into x0 with a register offset scaled by lsl #3.", m => { Longs1234(m); m.Regs[2] = 1; }, m => m.Regs[0] == 2, "ldr x0, [x1, x2, lsl #3]"),
        T(21, "x0 = 42. Push it with a pre-indexed store, then pop it into x1 with a post-indexed load. sp must end where it started.", m => m.Regs[0] = 42, m => m.Regs[1] == 42 && m.Sp == Machine.StackTop && m.Tape.Any(l => l.Text.Contains('!')), "str x0, [sp, #-16]! then ldr x1, [sp], #16"),
        T(22, "Four longs 1, 2, 3, 4 at 0x1000; x1 points there, x2 = 4. Sum them into x0 with a loop.", m => { Longs1234(m); m.Regs[2] = 4; }, m => m.Regs[0] == 10 && HasLoop(m), "loop: ldr x3, [x1], #8 · add x0, x0, x3 · sub x2, x2, #1 · cbnz x2, loop"),
        T(23, "x0 = 0xb6. Count its set bits into x1 with a loop.", m => m.Regs[0] = 0xb6, m => m.Regs[1] == 5 && HasLoop(m), "loop: and x2, x0, #1 · add x1, x1, x2 · lsr x0, x0, #1 · cbnz x0, loop"),
        T(24, "x1 points at \"hello\" at 0x1000. Its length into x0 with a loop over bytes.", m => { m.Directive("mem 0x1000 = \"hello\""); m.Regs[1] = 0x1000; }, m => m.Regs[0] == 5 && HasLoop(m), "loop: ldrb w2, [x1, x0] · cbz w2, done · add x0, x0, #1 · b loop · done:"),
        T(25, "x1 = 0x1020, x2 = 8. Fill 8 bytes at x1 with 0xff in a loop.", m => { m.Regs[1] = 0x1020; m.Regs[2] = 8; }, m => Enumerable.Range(0x1020, 8).All(a => m.Memory[a] == 0xff) && HasLoop(m), "mov w3, #0xff · loop: strb w3, [x1], #1 · subs x2, x2, #1 · b.ne loop"),
        T(26, "Four longs 1, 2, 3, 4 at 0x1000; x1 points there, x2 = 4. Reverse them in place.", m => { Longs1234(m); m.Regs[2] = 4; }, m => L(m, 0x1000) == 4 && L(m, 0x1008) == 3 && L(m, 0x1010) == 2 && L(m, 0x1018) == 1, "two pointers: x3 = x1, x4 = x1 + 24; swap with ldr/str, move them towards each other, stop when they cross"),
        T(27, "\"hello\" and its 0 sit at 0x1000; x1 = 0x1000, x2 = 0x1040, x3 = 6. Copy the six bytes to x2 in a loop.", m => { m.Directive("mem 0x1000 = \"hello\""); m.Regs[1] = 0x1000; m.Regs[2] = 0x1040; m.Regs[3] = 6; }, m => Enumerable.Range(0, 6).All(i => m.Memory[0x1040 + i] == m.Memory[0x1000 + i]) && HasLoop(m), "loop: ldrb w4, [x1], #1 · strb w4, [x2], #1 · subs x3, x3, #1 · b.ne loop"),
        T(28, "Write a function under a label double: x0 = x0 * 2, then ret. Call it with bl with x0 = 21. Type the call last.", m => m.Regs[0] = 21, m => m.Regs[0] == 42 && m.Tape.Any(l => l.Mnemonic == "bl") && m.Tape.Any(l => l.Mnemonic == "ret"), "b main · double: lsl x0, x0, #1 · ret · main: bl double"),
        T(29, "x19 = 0x1234 belongs to someone else. Sum 1 to 5 into x0 using x19 as the counter: save x19 on the stack first, restore it after. x19 and sp must end unchanged.", m => m.Regs[19] = 0x1234, m => m.Regs[0] == 15 && m.Regs[19] == 0x1234 && m.Sp == Machine.StackTop && m.Tape.Any(l => l.Mnemonic is "str" or "stp"), "str x19, [sp, #-16]! · mov x19, #5 · loop: add x0, x0, x19 · subs x19, x19, #1 · b.ne loop · ldr x19, [sp], #16"),
        T(30, "x0 = 5. Factorial of x0 into x0 with a recursive function: bl and ret, with a frame (stp x29, x30) so the recursion survives.", m => m.Regs[0] = 5, m => m.Regs[0] == 120 && m.Tape.Count(l => l.Mnemonic == "bl") >= 2 && m.Sp == Machine.StackTop, "b main · fact: cmp x0, #1 · b.le base · stp x29, x30, [sp, #-16]! · str x0, [sp, #-16]! · sub x0, x0, #1 · bl fact · ldr x1, [sp], #16 · mul x0, x0, x1 · ldp x29, x30, [sp], #16 · base: ret · main: bl fact"),
    ];

    private static void Longs1234(Machine m)
    {
        m.Directive("mem 0x1000 = 01 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00 03 00 00 00 00 00 00 00 04 00 00 00 00 00 00 00");
        m.Regs[1] = 0x1000;
    }

    private static bool HasLoop(Machine m) => m.Tape.Any(l => l.Mnemonic is "b" or "cbnz" or "cbz" or "tbnz" or "tbz" || (l.Mnemonic?.StartsWith("b.", StringComparison.Ordinal) ?? false));
}
