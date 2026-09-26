using System.Diagnostics;
using System.Numerics;
using Spectre.Console;

namespace Drill;

public enum AnswerForm { Hex, Bin, Dec, Bytes, YesNo }

public sealed record BitQuestion(string Kind, string Prompt, string Answer, AnswerForm Form);

/// Bits: quick-fire questions on what sits under the code. Hex and binary, two's
/// complement, shifts, masks, byte order, popcount, and what one ARM64 instruction does
/// to a register. Generated on the fly, so they never run out and never repeat.
public static class BitsMode
{
    public static readonly string[] Kinds = ["hex", "twos", "shift", "mask", "endian", "pop", "arm"];

    public static int Run(Store store, string? kind)
    {
        if (kind is not null && !Kinds.Contains(kind))
        {
            throw new DrillException($"no topic called {kind}. Topics: {string.Join(", ", Kinds)}");
        }
        var random = Random.Shared;
        var asked = 0;
        var right = 0;
        var seconds = 0.0;
        AnsiConsole.MarkupLine($"[bold]bits[/]{(kind is null ? "" : $" · {kind}")} · type the answer, Enter · q to stop");
        AnsiConsole.MarkupLine("[grey]hex with or without 0x, bytes space separated, yes or no where asked[/]");
        while (true)
        {
            var question = Generate(random, kind ?? Kinds[random.Next(Kinds.Length)]);
            AnsiConsole.Markup($"[grey]{question.Kind,-6}[/] {Markup.Escape(question.Prompt)}  ");
            var clock = Stopwatch.StartNew();
            var given = Console.ReadLine();
            clock.Stop();
            if (given is null || given.Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            var correct = Normalize(given, question.Form) == Normalize(question.Answer, question.Form);
            asked++;
            if (correct) right++;
            seconds += clock.Elapsed.TotalSeconds;
            store.LogBit(question.Kind, DateTime.Now, clock.Elapsed.TotalSeconds, correct, question.Prompt, question.Answer, given.Trim());
            AnsiConsole.MarkupLine(correct
                ? $"       [green]✓[/] [grey]{clock.Elapsed.TotalSeconds:0.0}s · {right}/{asked}[/]"
                : $"       [red]✗ {Markup.Escape(question.Answer)}[/] [grey]{clock.Elapsed.TotalSeconds:0.0}s · {right}/{asked}[/]");
        }
        if (asked > 0)
        {
            AnsiConsole.MarkupLine($"[bold]bits[/] {right}/{asked} · {100.0 * right / asked:0}% · {seconds / asked:0.0}s each");
        }
        return 0;
    }

    /// Answers compare after trimming, lower-casing, dropping 0x/0b prefixes, spaces and
    /// leading zeros; yes/no accept y and n.
    public static string Normalize(string text, AnswerForm form)
    {
        var t = text.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace(",", "");
        if (form == AnswerForm.YesNo)
        {
            return t is "y" or "yes" or "true" ? "yes" : t is "n" or "no" or "false" ? "no" : t;
        }
        if (form is AnswerForm.Hex or AnswerForm.Bytes && t.StartsWith("0x", StringComparison.Ordinal))
        {
            t = t[2..];
        }
        if (form == AnswerForm.Bin && t.StartsWith("0b", StringComparison.Ordinal))
        {
            t = t[2..];
        }
        var negative = t.StartsWith('-');
        if (negative) t = t[1..];
        t = t.TrimStart('0');
        if (t.Length == 0) t = "0";
        return negative && t != "0" ? "-" + t : t;
    }

    public static BitQuestion Generate(Random r, string kind) => kind switch
    {
        "hex" => Hex(r),
        "twos" => Twos(r),
        "shift" => Shift(r),
        "mask" => Mask(r),
        "endian" => Endian(r),
        "pop" => Pop(r),
        _ => Arm(r),
    };

    private static string H(long v) => v.ToString("x");
    private static string B(long v) => Convert.ToString(v, 2);

    private static BitQuestion Hex(Random r)
    {
        var n = r.Next(1, 256);
        var w = r.Next(256, 65536);
        return r.Next(6) switch
        {
            0 => new("hex", $"0x{n:x2} in binary", B(n), AnswerForm.Bin),
            1 => new("hex", $"0b{B(n).PadLeft(8, '0')} in hex", H(n), AnswerForm.Hex),
            2 => new("hex", $"0x{n:x2} in decimal", $"{n}", AnswerForm.Dec),
            3 => new("hex", $"{n} in hex", H(n), AnswerForm.Hex),
            4 => new("hex", $"{w} in hex", H(w), AnswerForm.Hex),
            _ => new("hex", $"0x{w:x4} in decimal", $"{w}", AnswerForm.Dec),
        };
    }

    private static BitQuestion Twos(Random r)
    {
        var bits = r.Next(2) == 0 ? 8 : 16;
        var half = 1L << (bits - 1);
        var full = 1L << bits;
        return r.Next(4) switch
        {
            0 => Neg(),
            1 => Signed(),
            2 => Unsigned(),
            _ => Negate(),
        };

        BitQuestion Neg()
        {
            var n = -r.NextInt64(1, half);
            return new("twos", $"{n} as {bits}-bit two's complement, in hex", H(full + n), AnswerForm.Hex);
        }
        BitQuestion Signed()
        {
            var u = r.NextInt64(half, full);
            return new("twos", $"0x{u:x} as a signed {bits}-bit value, in decimal", $"{u - full}", AnswerForm.Dec);
        }
        BitQuestion Unsigned()
        {
            var u = r.NextInt64(half, full);
            return new("twos", $"0x{u:x} as an unsigned {bits}-bit value, in decimal", $"{u}", AnswerForm.Dec);
        }
        BitQuestion Negate()
        {
            var u = r.NextInt64(1, full);
            return new("twos", $"negate 0x{u:x} in {bits}-bit two's complement, in hex", H((full - u) & (full - 1)), AnswerForm.Hex);
        }
    }

    private static BitQuestion Shift(Random r)
    {
        var n = r.Next(1, 256);
        var k = r.Next(1, 8);
        return r.Next(5) switch
        {
            0 => new("shift", $"0x{n:x2} << {k}, kept to 8 bits, in hex", H((n << k) & 0xff), AnswerForm.Hex),
            1 => new("shift", $"0x{n:x2} >> {k} logical, in hex", H(n >> k), AnswerForm.Hex),
            2 => new("shift", $"0x{n:x2} >> {k} arithmetic on a signed byte, in hex", H(((sbyte)n >> k) & 0xff), AnswerForm.Hex),
            3 => new("shift", $"rotate 0x{n:x2} right by {k}, 8-bit, in hex", H(((n >> k) | (n << (8 - k))) & 0xff), AnswerForm.Hex),
            _ => new("shift", $"0x{n:x2} << {k} in 8 bits: does a set bit fall off? (yes/no)", (n << k) > 0xff ? "yes" : "no", AnswerForm.YesNo),
        };
    }

    private static BitQuestion Mask(Random r)
    {
        var a = r.Next(0, 256);
        var b = r.Next(0, 256);
        var i = r.Next(0, 8);
        return r.Next(11) switch
        {
            0 => new("mask", $"0x{a:x2} & 0x{b:x2}, in hex", H(a & b), AnswerForm.Hex),
            1 => new("mask", $"0x{a:x2} | 0x{b:x2}, in hex", H(a | b), AnswerForm.Hex),
            2 => new("mask", $"0x{a:x2} ^ 0x{b:x2}, in hex", H(a ^ b), AnswerForm.Hex),
            3 => new("mask", $"~0x{a:x2}, 8-bit, in hex", H(~a & 0xff), AnswerForm.Hex),
            4 => new("mask", $"set bit {i} of 0x{a:x2}, in hex", H(a | (1 << i)), AnswerForm.Hex),
            5 => new("mask", $"clear bit {i} of 0x{a:x2}, in hex", H(a & ~(1 << i)), AnswerForm.Hex),
            6 => new("mask", $"toggle bit {i} of 0x{a:x2}, in hex", H(a ^ (1 << i)), AnswerForm.Hex),
            7 => new("mask", $"is bit {i} of 0x{a:x2} set? (yes/no)", (a >> i & 1) == 1 ? "yes" : "no", AnswerForm.YesNo),
            8 => new("mask", $"the mask that keeps the low {i + 1} bits, in hex", H((1 << (i + 1)) - 1), AnswerForm.Hex),
            9 => new("mask", $"the low nibble of 0x{a:x2}, in hex", H(a & 0xf), AnswerForm.Hex),
            _ => new("mask", $"the high nibble of 0x{a:x2}, in hex", H(a >> 4), AnswerForm.Hex),
        };
    }

    private static BitQuestion Endian(Random r)
    {
        var u = (uint)r.NextInt64(0x01000000, 0x100000000);
        var bytes = new[] { u & 0xff, (u >> 8) & 0xff, (u >> 16) & 0xff, u >> 24 };
        var little = string.Join(" ", bytes.Select(x => $"{x:x2}"));
        var big = string.Join(" ", bytes.Reverse().Select(x => $"{x:x2}"));
        var h = r.Next(0x0100, 0x10000);
        return r.Next(6) switch
        {
            0 => new("endian", $"0x{u:x8} stored little-endian: the bytes in memory order", little, AnswerForm.Bytes),
            1 => new("endian", $"0x{u:x8} stored big-endian: the bytes in memory order", big, AnswerForm.Bytes),
            2 => new("endian", $"bytes {little} in memory, read as a little-endian 32-bit value, in hex", H(u), AnswerForm.Hex),
            3 => new("endian", $"bytes {little} in memory, read as a big-endian 32-bit value, in hex", H(BinaryPrimitives(bytes)), AnswerForm.Hex),
            4 => new("endian", $"0x{h:x4} stored little-endian: the bytes in memory order", $"{h & 0xff:x2} {h >> 8:x2}", AnswerForm.Bytes),
            _ => new("endian", $"bytes {h & 0xff:x2} {h >> 8:x2} in memory, read as a little-endian 16-bit value, in hex", H(h), AnswerForm.Hex),
        };

        static long BinaryPrimitives(uint[] b) => ((long)b[0] << 24) | ((long)b[1] << 16) | ((long)b[2] << 8) | b[3];
    }

    private static BitQuestion Pop(Random r)
    {
        var n = r.Next(1, 256);
        var w = r.Next(256, 65536);
        var k = r.Next(0, 8);
        var u = (uint)r.NextInt64(1, 1L << r.Next(1, 33));
        return r.Next(8) switch
        {
            0 => new("pop", $"popcount of 0x{n:x2}", $"{BitOperations.PopCount((uint)n)}", AnswerForm.Dec),
            1 => new("pop", $"popcount of 0x{w:x4}", $"{BitOperations.PopCount((uint)w)}", AnswerForm.Dec),
            2 => new("pop", $"the lowest set bit of 0x{n:x2}, in hex", H(n & -n), AnswerForm.Hex),
            3 => r.Next(2) == 0
                ? new("pop", $"is 0x{1 << k:x2} a power of two? (yes/no)", "yes", AnswerForm.YesNo)
                : new("pop", $"is 0x{n:x2} a power of two? (yes/no)", BitOperations.IsPow2(n) ? "yes" : "no", AnswerForm.YesNo),
            4 => new("pop", $"clz of 0x{u:x8}, 32-bit", $"{BitOperations.LeadingZeroCount(u)}", AnswerForm.Dec),
            5 => new("pop", $"index of the highest set bit of 0x{n:x2}, bit 0 lowest", $"{31 - BitOperations.LeadingZeroCount((uint)n)}", AnswerForm.Dec),
            6 => new("pop", $"trailing zeros of 0x{n:x2}", $"{BitOperations.TrailingZeroCount(n)}", AnswerForm.Dec),
            _ => new("pop", $"0x{n:x2} with its lowest set bit cleared, in hex", H(n & (n - 1)), AnswerForm.Hex),
        };
    }

    private static readonly string[] Conditions = ["eq", "ne", "lt", "le", "gt", "ge"];

    private static bool Holds(string condition, long a, long b) => condition switch
    {
        "eq" => a == b,
        "ne" => a != b,
        "lt" => a < b,
        "le" => a <= b,
        "gt" => a > b,
        _ => a >= b,
    };

    private static BitQuestion Arm(Random r)
    {
        long a = r.Next(1, 0x80), b = r.Next(1, 0x80), c = r.Next(1, 0x40), d = r.Next(1, 0x40);
        var k = r.Next(1, 8);
        var cond = Conditions[r.Next(Conditions.Length)];
        var bytes = Enumerable.Range(0, 8).Select(_ => (long)r.Next(0, 256)).ToArray();
        var list = string.Join(" ", bytes.Select(x => $"{x:x2}"));
        var lo = r.Next(0, 0x10000);
        var hi = r.Next(1, 0x10000);
        var u8 = r.Next(0x80, 0x100);
        var u32 = (uint)r.NextInt64(0x01000000, 0x100000000);
        if (a < b) (a, b) = (b, a);
        return r.Next(27) switch
        {
            0 => new("arm", $"x1 = 0x{a:x}, x2 = 0x{b:x}: add x0, x1, x2 → x0 in hex", H(a + b), AnswerForm.Hex),
            1 => new("arm", $"x1 = 0x{a:x}, x2 = 0x{b:x}: sub x0, x1, x2 → x0 in hex", H(a - b), AnswerForm.Hex),
            2 => new("arm", $"x1 = 0x{a:x}, x2 = 0x{b:x}: and x0, x1, x2 → x0 in hex", H(a & b), AnswerForm.Hex),
            3 => new("arm", $"x1 = 0x{a:x}, x2 = 0x{b:x}: orr x0, x1, x2 → x0 in hex", H(a | b), AnswerForm.Hex),
            4 => new("arm", $"x1 = 0x{a:x}, x2 = 0x{b:x}: eor x0, x1, x2 → x0 in hex", H(a ^ b), AnswerForm.Hex),
            5 => new("arm", $"x1 = 0x{a:x}: lsl x0, x1, #{k} → x0 in hex", H(a << k), AnswerForm.Hex),
            6 => new("arm", $"x1 = 0x{a:x}: lsr x0, x1, #{k} → x0 in hex", H(a >> k), AnswerForm.Hex),
            7 => new("arm", $"x1 = -{a}: asr x0, x1, #{k} → x0 in decimal", $"{-a >> k}", AnswerForm.Dec),
            8 => new("arm", $"w1 = 0x{a:x}: mvn w0, w1 → w0 in hex", H(~a & 0xffffffff), AnswerForm.Hex),
            9 => new("arm", $"x1 = {a}: neg x0, x1 → x0 in decimal", $"{-a}", AnswerForm.Dec),
            10 => new("arm", $"x1 = {a}, x2 = {b}, x3 = {c}: madd x0, x1, x2, x3 → x0 in decimal", $"{a * b + c}", AnswerForm.Dec),
            11 => new("arm", $"x1 = {a}, x2 = {b}, x3 = {c}: msub x0, x1, x2, x3 → x0 in decimal", $"{c - a * b}", AnswerForm.Dec),
            12 => new("arm", $"x1 = {a}, x2 = {b}: udiv x0, x1, x2 → x0 in decimal", $"{a / b}", AnswerForm.Dec),
            13 => new("arm", $"x1 = {c}, x2 = {d}: cmp x1, x2 ; cset x0, {cond} → x0", Holds(cond, c, d) ? "1" : "0", AnswerForm.Dec),
            14 => new("arm", $"x1 = {a}, x2 = {b}, x3 = {c}, x4 = {d}: cmp x3, x4 ; csel x0, x1, x2, {cond} → x0 in decimal", $"{(Holds(cond, c, d) ? a : b)}", AnswerForm.Dec),
            15 => Ubfx(),
            16 => new("arm", $"x1 → bytes {list}: ldrb w0, [x1, #{k}] → w0 in hex", H(bytes[k]), AnswerForm.Hex),
            17 => new("arm", $"x1 → bytes {list}: ldr w0, [x1] → w0 in hex", H(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24), AnswerForm.Hex),
            18 => new("arm", $"x1 → bytes {list}: ldrh w0, [x1, #{(k / 2) * 2}] → w0 in hex", H(bytes[(k / 2) * 2] | bytes[(k / 2) * 2 + 1] << 8), AnswerForm.Hex),
            19 => new("arm", $"movz x0, #0x{lo:x} ; movk x0, #0x{hi:x}, lsl #16 → x0 in hex", H(((long)hi << 16) | (uint)lo), AnswerForm.Hex),
            20 => new("arm", $"x1 = 0x{a:x}, x2 = 0x{b:x}: add x0, x1, x2, lsl #{k % 3 + 1} → x0 in hex", H(a + (b << (k % 3 + 1))), AnswerForm.Hex),
            21 => new("arm", $"x1 = {c}, x2 = {d}: cmp x1, x2 ; b.{cond} — taken? (yes/no)", Holds(cond, c, d) ? "yes" : "no", AnswerForm.YesNo),
            22 => new("arm", $"x1 = {c}, x2 = {d}: cmp x1, x2 → flags N Z C V as four bits", $"{(c - d < 0 ? 1 : 0)}{(c == d ? 1 : 0)}{(c >= d ? 1 : 0)}0", AnswerForm.Bin),
            23 => new("arm", $"x1 = 0x{a:x}, x2 = 0x{b:x}: bic x0, x1, x2 → x0 in hex", H(a & ~b), AnswerForm.Hex),
            24 => new("arm", $"w1 = 0x{u32:x8}: rev w0, w1 → w0 in hex", H(BinaryPrimitivesReverse(u32)), AnswerForm.Hex),
            25 => new("arm", $"w1 = 0x{u8:x2}: sxtb w0, w1 → w0 in hex", H(((long)(sbyte)u8) & 0xffffffff), AnswerForm.Hex),
            _ => new("arm", $"w1 = 0x{u8:x2}: uxtb w0, w1 → w0 in hex", H(u8), AnswerForm.Hex),
        };

        BitQuestion Ubfx()
        {
            var lsb = r.Next(0, 6);
            var width = r.Next(1, 9 - lsb);
            return new("arm", $"x1 = 0x{a:x2}: ubfx x0, x1, #{lsb}, #{width} → x0 in hex", H((a >> lsb) & ((1L << width) - 1)), AnswerForm.Hex);
        }

        static long BinaryPrimitivesReverse(uint v) => ((v & 0xff) << 24) | ((v & 0xff00) << 8) | ((v >> 8) & 0xff00) | (v >> 24);
    }
}
