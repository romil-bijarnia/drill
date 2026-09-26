using System.Globalization;
using System.Text;

namespace Drill;

public sealed class MachineException(string message) : Exception(message);

/// One line of the tape: a label, an instruction, or a directive such as `x1 = 5`.
public sealed record TapeLine(string Text, string? Label, string? Mnemonic, string[] Operands, string? Directive);

/// A small ARM64: thirty-one general registers, sp, the NZCV flags, 64 KiB of memory and
/// the integer instructions the snippet bank uses. It runs a tape of lines you typed,
/// so a backward branch replays what is already there and you can watch a loop go round.
public sealed class Machine
{
    public const int MemorySize = 0x10000;
    public const long DataBase = 0x1000;
    public const long StackTop = 0xF000;
    public const int StepLimit = 100_000;

    public readonly long[] Regs = new long[31];
    public long Sp = StackTop;
    public bool N, Z, C, V;
    public readonly byte[] Memory = new byte[MemorySize];
    public readonly HashSet<int> TouchedRegs = [];
    public readonly HashSet<long> TouchedBytes = [];
    public bool SpTouched;
    public bool FlagsTouched;
    public long Focus = DataBase;
    public long Steps;
    public string? Waiting;
    public int WaitingAt;
    public bool FlagsEverSet;

    public readonly List<TapeLine> Tape = [];

    public void Reset()
    {
        Array.Clear(Regs);
        Sp = StackTop;
        N = Z = C = V = false;
        Array.Clear(Memory);
        ClearTouched();
        Focus = DataBase;
        Steps = 0;
        Waiting = null;
        FlagsEverSet = false;
    }

    public void ClearTouched()
    {
        TouchedRegs.Clear();
        TouchedBytes.Clear();
        SpTouched = false;
        FlagsTouched = false;
    }

    // MARK: parsing

    public static TapeLine Parse(string text)
    {
        var line = text.Trim();
        var comment = line.IndexOf("//", StringComparison.Ordinal);
        if (comment >= 0) line = line[..comment].Trim();
        comment = line.IndexOf(';');
        if (comment >= 0) line = line[..comment].Trim();
        if (line.Length == 0)
        {
            throw new MachineException("nothing to run");
        }
        if (IsDirective(line))
        {
            return new TapeLine(line, null, null, [], line);
        }
        string? label = null;
        var colon = line.IndexOf(':');
        if (colon > 0 && !line[..colon].Contains('[') && !line[..colon].Contains(' ') && !line[..colon].Contains(','))
        {
            label = line[..colon].Trim();
            line = line[(colon + 1)..].Trim();
            if (line.Length == 0)
            {
                return new TapeLine(text.Trim(), label, null, [], null);
            }
        }
        var space = line.IndexOf(' ');
        var mnemonic = (space < 0 ? line : line[..space]).ToLowerInvariant();
        var rest = space < 0 ? "" : line[(space + 1)..].Trim();
        return new TapeLine(text.Trim(), label, mnemonic, SplitOperands(rest), null);
    }

    private static bool IsDirective(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.StartsWith("mem ", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(lower, @"^(x\d+|w\d+|sp|lr|fp)\s*=");
    }

    private static string[] SplitOperands(string text)
    {
        var parts = new List<string>();
        var depth = 0;
        var current = new StringBuilder();
        var quote = false;
        foreach (var c in text)
        {
            if (c == '\'') quote = !quote;
            if (c == '[') depth++;
            if (c == ']') depth--;
            if (c == ',' && depth == 0 && !quote)
            {
                parts.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.ToString().Trim().Length > 0)
        {
            parts.Add(current.ToString().Trim());
        }
        return parts.ToArray();
    }

    private readonly record struct Reg(int Index, bool W, bool IsSp, bool IsZero);

    private static Reg ParseReg(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        switch (t)
        {
            case "sp": return new Reg(-1, false, true, false);
            case "wsp": return new Reg(-1, true, true, false);
            case "xzr": return new Reg(31, false, false, true);
            case "wzr": return new Reg(31, true, false, true);
            case "lr": return new Reg(30, false, false, false);
            case "fp": return new Reg(29, false, false, false);
        }
        if ((t.StartsWith('x') || t.StartsWith('w')) && int.TryParse(t[1..], out var n) && n is >= 0 and <= 30)
        {
            return new Reg(n, t[0] == 'w', false, false);
        }
        throw new MachineException($"'{text}' is not a register (x0–x30, w0–w30, sp, lr, fp, xzr)");
    }

    private static bool LooksLikeReg(string text)
    {
        try { ParseReg(text); return true; } catch (MachineException) { return false; }
    }

    public static long ParseImm(string text)
    {
        var t = text.Trim();
        if (t.StartsWith('#')) t = t[1..].Trim();
        var negative = t.StartsWith('-');
        if (negative) t = t[1..].Trim();
        var invert = t.StartsWith('~');
        if (invert) t = t[1..].Trim();
        long value;
        if (t.Length == 3 && t[0] == '\'' && t[2] == '\'')
        {
            value = t[1];
        }
        else if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = (long)ulong.Parse(t[2..].Replace("_", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
        else if (t.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToInt64(t[2..].Replace("_", ""), 2);
        }
        else if (!long.TryParse(t.Replace("_", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            throw new MachineException($"'{text}' is not a number (try #5, #-8, #0x1f, #0b101, #'a')");
        }
        if (invert) value = ~value;
        return negative ? -value : value;
    }

    private static bool LooksLikeImm(string text)
    {
        var t = text.Trim();
        return t.StartsWith('#') || t.StartsWith('-') || t.Length > 0 && char.IsDigit(t[0]) || t.StartsWith('\'');
    }

    // MARK: registers and memory

    private long Get(Reg r)
    {
        if (r.IsZero) return 0;
        var value = r.IsSp ? Sp : Regs[r.Index];
        return r.W ? value & 0xffffffffL : value;
    }

    private void Set(Reg r, long value)
    {
        if (r.IsZero) return;
        var stored = r.W ? value & 0xffffffffL : value;
        if (r.IsSp)
        {
            Sp = stored;
            SpTouched = true;
        }
        else
        {
            Regs[r.Index] = stored;
            TouchedRegs.Add(r.Index);
        }
    }

    public long Load(long address, int size, bool signed)
    {
        Check(address, size);
        long value = 0;
        for (var i = size - 1; i >= 0; i--)
        {
            value = (value << 8) | Memory[address + i];
        }
        if (signed && size < 8)
        {
            var shift = 64 - size * 8;
            value = (value << shift) >> shift;
        }
        Follow(address);
        return value;
    }

    public void Store(long address, int size, long value)
    {
        Check(address, size);
        for (var i = 0; i < size; i++)
        {
            Memory[address + i] = (byte)(value >> (8 * i));
            TouchedBytes.Add(address + i);
        }
        Follow(address);
    }

    private static void Check(long address, int size)
    {
        if (address < 0 || address + size > MemorySize)
        {
            throw new MachineException($"segfault: address 0x{address:x} is outside this machine's memory (0x0–0x{MemorySize - 1:x})");
        }
    }

    private void Follow(long address)
    {
        if (address < Focus || address >= Focus + 64)
        {
            Focus = address & ~0xFL;
        }
    }

    // MARK: flags

    private void FlagsFrom(long a, long b, bool subtract, bool w)
    {
        if (w)
        {
            uint ua = (uint)a, ub = (uint)b;
            var r = subtract ? ua - ub : ua + ub;
            N = (int)r < 0;
            Z = r == 0;
            C = subtract ? ua >= ub : r < ua;
            V = subtract ? ((int)ua < 0) != ((int)ub < 0) && ((int)r < 0) != ((int)ua < 0)
                         : ((int)ua < 0) == ((int)ub < 0) && ((int)r < 0) != ((int)ua < 0);
        }
        else
        {
            ulong ua = (ulong)a, ub = (ulong)b;
            var r = subtract ? ua - ub : ua + ub;
            N = (long)r < 0;
            Z = r == 0;
            C = subtract ? ua >= ub : r < ua;
            V = subtract ? ((long)ua < 0) != ((long)ub < 0) && ((long)r < 0) != ((long)ua < 0)
                         : ((long)ua < 0) == ((long)ub < 0) && ((long)r < 0) != ((long)ua < 0);
        }
        FlagsTouched = true;
        FlagsEverSet = true;
    }

    private void FlagsLogical(long result, bool w)
    {
        N = w ? (int)result < 0 : result < 0;
        Z = (w ? result & 0xffffffffL : result) == 0;
        C = false;
        V = false;
        FlagsTouched = true;
        FlagsEverSet = true;
    }

    public bool Holds(string condition) => condition.ToLowerInvariant() switch
    {
        "eq" => Z,
        "ne" => !Z,
        "cs" or "hs" => C,
        "cc" or "lo" => !C,
        "mi" => N,
        "pl" => !N,
        "vs" => V,
        "vc" => !V,
        "hi" => C && !Z,
        "ls" => !(C && !Z),
        "ge" => N == V,
        "lt" => N != V,
        "gt" => !Z && N == V,
        "le" => !(!Z && N == V),
        "al" or "nv" => true,
        _ => throw new MachineException($"'{condition}' is not a condition (eq ne lt le gt ge mi pl hi ls cs cc vs vc)"),
    };

    private static string Invert(string condition) => condition.ToLowerInvariant() switch
    {
        "eq" => "ne", "ne" => "eq", "cs" or "hs" => "cc", "cc" or "lo" => "cs", "mi" => "pl", "pl" => "mi",
        "vs" => "vc", "vc" => "vs", "hi" => "ls", "ls" => "hi", "ge" => "lt", "lt" => "ge", "gt" => "le", "le" => "gt",
        _ => "nv",
    };

    // MARK: operands

    private long Operand(string text, bool w)
    {
        if (LooksLikeReg(text)) return Get(ParseReg(text));
        return ParseImm(text);
    }

    private long Shifted(string[] ops, int index, bool w)
    {
        var value = Operand(ops[index], w);
        if (ops.Length > index + 1 && !ops[index + 1].StartsWith('['))
        {
            var spec = ops[index + 1].Trim().ToLowerInvariant();
            var parts = spec.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0] is "lsl" or "lsr" or "asr" or "ror")
            {
                var amount = (int)ParseImm(parts[1]);
                value = ApplyShift(parts[0], value, amount, w);
            }
            else if (parts.Length >= 1 && parts[0] is "sxtw" or "uxtw" or "sxtb" or "uxtb" or "sxth" or "uxth")
            {
                value = Extend(parts[0], value);
                if (parts.Length == 2) value <<= (int)ParseImm(parts[1]);
            }
            else
            {
                throw new MachineException($"'{ops[index + 1]}' is not a shift (lsl, lsr, asr, ror #n)");
            }
        }
        return w ? value & 0xffffffffL : value;
    }

    private static long ApplyShift(string kind, long value, int amount, bool w)
    {
        var bits = w ? 32 : 64;
        amount &= bits - 1;
        if (w) value &= 0xffffffffL;
        return kind switch
        {
            "lsl" => value << amount,
            "lsr" => (long)((ulong)value >> amount),
            "asr" => w ? (long)((int)value >> amount) : value >> amount,
            "ror" => w ? (long)(((uint)value >> amount) | ((uint)value << (32 - amount))) & 0xffffffffL
                       : (long)(((ulong)value >> amount) | ((ulong)value << (64 - amount))),
            _ => value,
        };
    }

    private static long Extend(string kind, long value) => kind switch
    {
        "sxtb" => (sbyte)value,
        "sxth" => (short)value,
        "sxtw" => (int)value,
        "uxtb" => value & 0xff,
        "uxth" => value & 0xffff,
        _ => value & 0xffffffffL,
    };

    /// The address of a memory operand, applying pre- and post-index writeback.
    private long Address(string[] ops, int index, bool writeBackAfter, out int consumed)
    {
        consumed = 1;
        var text = ops[index].Trim();
        if (!text.StartsWith('['))
        {
            throw new MachineException($"'{text}' should be a memory operand like [x1], [x1, #8], [x1, #8]!, [x1], #8 or [x1, x2, lsl #3]");
        }
        var pre = text.EndsWith('!');
        var inner = text.TrimEnd('!').Trim();
        inner = inner[1..^1];
        var parts = SplitOperands(inner);
        var baseReg = ParseReg(parts[0]);
        var baseValue = Get(baseReg);
        long offset = 0;
        if (parts.Length >= 2)
        {
            if (LooksLikeReg(parts[1]))
            {
                var idx = ParseReg(parts[1]);
                offset = Get(idx);
                if (parts.Length == 3)
                {
                    var spec = parts[2].Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    offset = spec[0] switch
                    {
                        "lsl" => offset << (int)ParseImm(spec[1]),
                        "sxtw" => (int)offset << (spec.Length > 1 ? (int)ParseImm(spec[1]) : 0),
                        "uxtw" => (offset & 0xffffffffL) << (spec.Length > 1 ? (int)ParseImm(spec[1]) : 0),
                        _ => throw new MachineException($"'{parts[2]}' is not an index extension (lsl #n, sxtw, uxtw)"),
                    };
                }
            }
            else
            {
                offset = ParseImm(parts[1]);
            }
        }
        var address = baseValue + offset;
        if (pre)
        {
            Set(baseReg, address);
        }
        else if (ops.Length > index + 1 && LooksLikeImm(ops[index + 1]))
        {
            consumed = 2;
            if (writeBackAfter)
            {
                Set(baseReg, baseValue + ParseImm(ops[index + 1]));
            }
            return baseValue;
        }
        return address;
    }

    // MARK: execution

    public int IndexOfLabel(string name, int from)
    {
        var n = name.Trim();
        if (n.Length >= 2 && char.IsDigit(n[0]) && (n[^1] == 'f' || n[^1] == 'b'))
        {
            var number = n[..^1];
            if (n[^1] == 'f')
            {
                for (var i = from + 1; i < Tape.Count; i++) if (Tape[i].Label == number) return i;
            }
            else
            {
                for (var i = from; i >= 0; i--) if (Tape[i].Label == number) return i;
            }
            return -1;
        }
        for (var i = 0; i < Tape.Count; i++)
        {
            if (Tape[i].Label == n) return i;
        }
        return -1;
    }

    /// Runs the tape from an index until it falls off the end, returns to nobody, or
    /// stops at a branch whose label does not exist yet. `onStep` sees each executed index.
    public void Run(int from, Action<int>? onStep = null)
    {
        Waiting = null;
        var pc = from;
        var steps = 0;
        while (pc >= 0 && pc < Tape.Count)
        {
            if (++steps > StepLimit)
            {
                throw new MachineException($"stopped after {StepLimit} steps: that loops forever");
            }
            onStep?.Invoke(pc);
            var next = Execute(Tape[pc], pc);
            if (next == Waits)
            {
                return;
            }
            pc = next;
            Steps++;
        }
    }

    private const int Waits = -2;
    private const int Stop = -1;

    private int Branch(string label, int pc)
    {
        var target = IndexOfLabel(label, pc);
        if (target < 0)
        {
            Waiting = label.Trim();
            WaitingAt = pc;
            return Waits;
        }
        return target;
    }

    public void Directive(string text)
    {
        var lower = text.Trim();
        if (lower.StartsWith("mem ", StringComparison.OrdinalIgnoreCase))
        {
            var eq = lower.IndexOf('=');
            if (eq < 0) throw new MachineException("mem 0x1000 = 48 65 6c   or   mem 0x1000 = \"hello\"");
            var address = ParseImm(lower[4..eq]);
            var rhs = lower[(eq + 1)..].Trim();
            if (rhs.StartsWith('"') && rhs.EndsWith('"') && rhs.Length >= 2)
            {
                var bytes = Encoding.ASCII.GetBytes(rhs[1..^1]);
                for (var i = 0; i < bytes.Length; i++) Store(address + i, 1, bytes[i]);
                Store(address + bytes.Length, 1, 0);
            }
            else
            {
                var i = 0;
                foreach (var part in rhs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    Store(address + i++, 1, (long)ulong.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
            }
            return;
        }
        var parts = lower.Split('=', 2);
        var reg = ParseReg(parts[0]);
        Set(reg, ParseImm(parts[1]));
    }

    private int Execute(TapeLine line, int pc)
    {
        if (line.Directive is not null)
        {
            Directive(line.Directive);
            return pc + 1;
        }
        if (line.Mnemonic is null)
        {
            return pc + 1;
        }
        var m = line.Mnemonic;
        var ops = line.Operands;
        Reg Rd(int i) => ParseReg(Need(ops, i, m));
        bool W(int i) => Rd(i).W;

        if (m.StartsWith("b.", StringComparison.Ordinal))
        {
            return Holds(m[2..]) ? Branch(Need(ops, 0, m), pc) : pc + 1;
        }
        switch (m)
        {
            case "nop":
                return pc + 1;
            case "b":
                return Branch(Need(ops, 0, m), pc);
            case "bl":
                Regs[30] = pc + 1;
                TouchedRegs.Add(30);
                return Branch(Need(ops, 0, m), pc);
            case "ret":
                {
                    var target = ops.Length > 0 ? Get(Rd(0)) : Regs[30];
                    return target == 0 ? Stop : (int)target;
                }
            case "cbz":
                return Get(Rd(0)) == 0 ? Branch(Need(ops, 1, m), pc) : pc + 1;
            case "cbnz":
                return Get(Rd(0)) != 0 ? Branch(Need(ops, 1, m), pc) : pc + 1;
            case "tbz":
                return ((Get(Rd(0)) >> (int)ParseImm(Need(ops, 1, m))) & 1) == 0 ? Branch(Need(ops, 2, m), pc) : pc + 1;
            case "tbnz":
                return ((Get(Rd(0)) >> (int)ParseImm(Need(ops, 1, m))) & 1) != 0 ? Branch(Need(ops, 2, m), pc) : pc + 1;

            case "mov":
                Set(Rd(0), Operand(Need(ops, 1, m), W(0)));
                return pc + 1;
            case "movz":
                Set(Rd(0), ParseImm(Need(ops, 1, m)) << ShiftAmount(ops, 2));
                return pc + 1;
            case "movn":
                Set(Rd(0), ~(ParseImm(Need(ops, 1, m)) << ShiftAmount(ops, 2)));
                return pc + 1;
            case "movk":
                {
                    var rd = Rd(0);
                    var shift = ShiftAmount(ops, 2);
                    var mask = 0xffffL << shift;
                    Set(rd, (Get(rd) & ~mask) | ((ParseImm(Need(ops, 1, m)) & 0xffff) << shift));
                    return pc + 1;
                }
            case "mvn":
                Set(Rd(0), ~Shifted(ops, 1, W(0)));
                return pc + 1;

            case "add" or "adds" or "sub" or "subs":
                {
                    var rd = Rd(0);
                    var a = Get(ParseReg(Need(ops, 1, m)));
                    var b = Shifted(ops, 2, rd.W);
                    var subtract = m.StartsWith("sub", StringComparison.Ordinal);
                    if (m.EndsWith('s')) FlagsFrom(a, b, subtract, rd.W);
                    Set(rd, subtract ? a - b : a + b);
                    return pc + 1;
                }
            case "cmp" or "cmn":
                {
                    var rn = Rd(0);
                    FlagsFrom(Get(rn), Shifted(ops, 1, rn.W), m == "cmp", rn.W);
                    return pc + 1;
                }
            case "neg" or "negs":
                {
                    var rd = Rd(0);
                    var b = Shifted(ops, 1, rd.W);
                    if (m == "negs") FlagsFrom(0, b, true, rd.W);
                    Set(rd, -b);
                    return pc + 1;
                }
            case "mul":
                Set(Rd(0), Get(Rd(1)) * Get(Rd(2)));
                return pc + 1;
            case "mneg":
                Set(Rd(0), -(Get(Rd(1)) * Get(Rd(2))));
                return pc + 1;
            case "madd":
                Set(Rd(0), Get(Rd(1)) * Get(Rd(2)) + Get(Rd(3)));
                return pc + 1;
            case "msub":
                Set(Rd(0), Get(Rd(3)) - Get(Rd(1)) * Get(Rd(2)));
                return pc + 1;
            case "sdiv":
                {
                    var rd = Rd(0);
                    var a = Get(Rd(1));
                    var b = Get(Rd(2));
                    if (rd.W) { a = (int)a; b = (int)b; }
                    Set(rd, b == 0 ? 0 : a == long.MinValue && b == -1 ? a : a / b);
                    return pc + 1;
                }
            case "udiv":
                {
                    var rd = Rd(0);
                    var a = (ulong)Get(Rd(1));
                    var b = (ulong)Get(Rd(2));
                    Set(rd, b == 0 ? 0 : (long)(a / b));
                    return pc + 1;
                }

            case "and" or "ands" or "orr" or "eor" or "bic" or "bics" or "orn" or "eon":
                {
                    var rd = Rd(0);
                    var a = Get(ParseReg(Need(ops, 1, m)));
                    var b = Shifted(ops, 2, rd.W);
                    var result = m switch
                    {
                        "and" or "ands" => a & b,
                        "orr" => a | b,
                        "eor" => a ^ b,
                        "bic" or "bics" => a & ~b,
                        "orn" => a | ~b,
                        _ => a ^ ~b,
                    };
                    if (m.EndsWith('s')) FlagsLogical(result, rd.W);
                    Set(rd, result);
                    return pc + 1;
                }
            case "tst":
                {
                    var rn = Rd(0);
                    FlagsLogical(Get(rn) & Shifted(ops, 1, rn.W), rn.W);
                    return pc + 1;
                }
            case "lsl" or "lsr" or "asr" or "ror":
                {
                    var rd = Rd(0);
                    var amount = (int)Operand(Need(ops, 2, m), rd.W);
                    Set(rd, ApplyShift(m, Get(ParseReg(Need(ops, 1, m))), amount, rd.W));
                    return pc + 1;
                }

            case "cset":
                Set(Rd(0), Holds(Need(ops, 1, m)) ? 1 : 0);
                return pc + 1;
            case "csetm":
                Set(Rd(0), Holds(Need(ops, 1, m)) ? -1 : 0);
                return pc + 1;
            case "cinc":
                Set(Rd(0), Holds(Need(ops, 2, m)) ? Get(Rd(1)) + 1 : Get(Rd(1)));
                return pc + 1;
            case "cinv":
                Set(Rd(0), Holds(Need(ops, 2, m)) ? ~Get(Rd(1)) : Get(Rd(1)));
                return pc + 1;
            case "cneg":
                Set(Rd(0), Holds(Need(ops, 2, m)) ? -Get(Rd(1)) : Get(Rd(1)));
                return pc + 1;
            case "csel":
                Set(Rd(0), Holds(Need(ops, 3, m)) ? Get(Rd(1)) : Get(Rd(2)));
                return pc + 1;
            case "csinc":
                Set(Rd(0), Holds(Need(ops, 3, m)) ? Get(Rd(1)) : Get(Rd(2)) + 1);
                return pc + 1;
            case "csinv":
                Set(Rd(0), Holds(Need(ops, 3, m)) ? Get(Rd(1)) : ~Get(Rd(2)));
                return pc + 1;
            case "csneg":
                Set(Rd(0), Holds(Need(ops, 3, m)) ? Get(Rd(1)) : -Get(Rd(2)));
                return pc + 1;

            case "ldr" or "ldur" or "ldrb" or "ldurb" or "ldrh" or "ldurh" or "ldrsb" or "ldrsh" or "ldrsw":
                {
                    var rt = Rd(0);
                    var size = m switch { "ldrb" or "ldurb" or "ldrsb" => 1, "ldrh" or "ldurh" or "ldrsh" => 2, "ldrsw" => 4, _ => rt.W ? 4 : 8 };
                    var signed = m.StartsWith("ldrs", StringComparison.Ordinal);
                    var address = Address(ops, 1, true, out _);
                    Set(rt, Load(address, size, signed));
                    return pc + 1;
                }
            case "str" or "stur" or "strb" or "sturb" or "strh" or "sturh":
                {
                    var rt = Rd(0);
                    var size = m switch { "strb" or "sturb" => 1, "strh" or "sturh" => 2, _ => rt.W ? 4 : 8 };
                    var address = Address(ops, 1, true, out _);
                    Store(address, size, Get(rt));
                    return pc + 1;
                }
            case "ldp":
                {
                    var r1 = Rd(0);
                    var r2 = Rd(1);
                    var size = r1.W ? 4 : 8;
                    var address = Address(ops, 2, true, out _);
                    Set(r1, Load(address, size, false));
                    Set(r2, Load(address + size, size, false));
                    return pc + 1;
                }
            case "stp":
                {
                    var r1 = Rd(0);
                    var r2 = Rd(1);
                    var size = r1.W ? 4 : 8;
                    var address = Address(ops, 2, true, out _);
                    Store(address, size, Get(r1));
                    Store(address + size, size, Get(r2));
                    return pc + 1;
                }

            case "clz":
                {
                    var rd = Rd(0);
                    var v = Get(Rd(1));
                    Set(rd, rd.W ? System.Numerics.BitOperations.LeadingZeroCount((uint)v) : System.Numerics.BitOperations.LeadingZeroCount((ulong)v));
                    return pc + 1;
                }
            case "rbit":
                {
                    var rd = Rd(0);
                    var v = (ulong)Get(Rd(1));
                    ulong r = 0;
                    var bits = rd.W ? 32 : 64;
                    for (var i = 0; i < bits; i++) r |= ((v >> i) & 1) << (bits - 1 - i);
                    Set(rd, (long)r);
                    return pc + 1;
                }
            case "rev":
                {
                    var rd = Rd(0);
                    var v = (ulong)Get(Rd(1));
                    Set(rd, rd.W ? (long)System.Buffers.Binary.BinaryPrimitives.ReverseEndianness((uint)v) : (long)System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v));
                    return pc + 1;
                }
            case "rev16":
                {
                    var rd = Rd(0);
                    var v = (ulong)Get(Rd(1));
                    ulong r = 0;
                    for (var i = 0; i < 8; i += 2) r |= (((v >> (8 * i)) & 0xff) << (8 * (i + 1))) | (((v >> (8 * (i + 1))) & 0xff) << (8 * i));
                    Set(rd, (long)r);
                    return pc + 1;
                }
            case "sxtb" or "sxth" or "sxtw" or "uxtb" or "uxth":
                Set(Rd(0), Extend(m, Get(Rd(1))));
                return pc + 1;
            case "ubfx" or "sbfx":
                {
                    var rd = Rd(0);
                    var lsb = (int)ParseImm(Need(ops, 2, m));
                    var width = (int)ParseImm(Need(ops, 3, m));
                    var field = (long)(((ulong)Get(Rd(1)) >> lsb) & (width == 64 ? ulong.MaxValue : (1UL << width) - 1));
                    if (m == "sbfx" && width < 64 && ((field >> (width - 1)) & 1) == 1) field |= -1L << width;
                    Set(rd, field);
                    return pc + 1;
                }
            case "ubfiz" or "sbfiz":
                {
                    var rd = Rd(0);
                    var lsb = (int)ParseImm(Need(ops, 2, m));
                    var width = (int)ParseImm(Need(ops, 3, m));
                    var field = Get(Rd(1)) & ((1L << width) - 1);
                    if (m == "sbfiz" && ((field >> (width - 1)) & 1) == 1) field |= -1L << width;
                    Set(rd, field << lsb);
                    return pc + 1;
                }
            case "bfi":
                {
                    var rd = Rd(0);
                    var lsb = (int)ParseImm(Need(ops, 2, m));
                    var width = (int)ParseImm(Need(ops, 3, m));
                    var mask = ((1L << width) - 1) << lsb;
                    Set(rd, (Get(rd) & ~mask) | ((Get(Rd(1)) << lsb) & mask));
                    return pc + 1;
                }
            case "extr":
                {
                    var rd = Rd(0);
                    var lsb = (int)ParseImm(Need(ops, 3, m));
                    var hi = (ulong)Get(Rd(1));
                    var lo = (ulong)Get(Rd(2));
                    var bits = rd.W ? 32 : 64;
                    var r = lsb == 0 ? lo : (lo >> lsb) | (hi << (bits - lsb));
                    Set(rd, (long)r);
                    return pc + 1;
                }
            case "adr":
            case "adrp":
                throw new MachineException($"{m} needs real addresses; on this machine put the address in with mov (data lives at 0x1000)");
            case "svc":
                throw new MachineException("no operating system on this machine, so no svc");
            case "fmov" or "cnt" or "addv" or "uaddlv":
                throw new MachineException($"{m} is a SIMD instruction; this machine only has the integer set");
            default:
                throw new MachineException($"'{m}' is not an instruction this machine knows (type help for the list)");
        }
    }

    private static int ShiftAmount(string[] ops, int index)
    {
        if (ops.Length <= index) return 0;
        var parts = ops[index].Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && parts[0] == "lsl") return (int)ParseImm(parts[1]);
        throw new MachineException($"'{ops[index]}' should be lsl #16, #32 or #48");
    }

    private static string Need(string[] ops, int index, string mnemonic)
    {
        if (ops.Length <= index)
        {
            throw new MachineException($"{mnemonic} needs more operands (it has {ops.Length})");
        }
        return ops[index];
    }

    public static readonly string Instructions =
        "mov movz movk movn mvn · add adds sub subs cmp cmn neg · mul madd msub mneg sdiv udiv · " +
        "and ands orr eor bic orn tst · lsl lsr asr ror · cset csetm cinc cneg csel csinc csinv csneg · " +
        "b b.cond cbz cbnz tbz tbnz bl ret · ldr ldrb ldrh ldrsb ldrsh ldrsw ldur str strb strh stur ldp stp · " +
        "clz rbit rev rev16 sxtb sxth sxtw uxtb uxth ubfx sbfx bfi ubfiz extr nop";
}
