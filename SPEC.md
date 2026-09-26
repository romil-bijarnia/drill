# Etude

A local trainer that moves me from copying C# to producing it. Every session
is a short set of reps. The tool measures one thing: can I write correct code
with nothing in front of me.

## Modes

| Mode   | What I see                          | What I do                        | Scored on                    |
|--------|-------------------------------------|----------------------------------|------------------------------|
| Trace  | Faint reference code                | Type over it, per-char colouring | Accuracy, seconds            |
| Recall | Snippet for 10 seconds, then blank  | Type it from memory              | Edit distance to original    |
| Blank  | One-line spec plus tests            | Write the function               | Tests pass, first try or not |

Every snippet can be practised in all three modes. Trace trains the hands,
recall trains memory, blank trains the head. Blank is the only mode that
matters for the weekly number.

## Scheduling

Leitner boxes. Each snippet sits in box 1 to 5.

- Pass: box + 1 (max 5). Fail: box = 1.
- A snippet in box n is due every 2^(n-1) days: 1, 2, 4, 8, 16.
- A daily session is "everything due today," capped at 15 snippets.
- Trace passes at 97% accuracy or better. Recall passes at edit distance 0
  after whitespace normalisation. Blank passes when all tests pass.

## Snippet bank

Snippets live in `snippets/` as Markdown files, one per snippet.

```
---
id: 003
title: Generic max with constraint
tags: [generics, constraints]
modes: [trace, recall, blank]
spec: Return the larger of two values of any comparable type.
tests:
  - call: 'Max(3, 7)'
    expect: '7'
  - call: 'Max("apple", "pear")'
    expect: 'pear'
  - call: 'Max(2.5, 2.5)'
    expect: '2.5'
---
public static T Max<T>(T a, T b) where T : IComparable<T>
    => a.CompareTo(b) >= 0 ? a : b;
```

- `modes` lists which modes may present this snippet.
- `spec` is the single line shown in blank mode.
- `tests` are C# expressions evaluated against my code. A test passes when
  the expression's `ToString()` equals `expect`.
- Everything below the front matter is the reference code.

Sources for snippets: C# idioms I fumble, and functions pulled from my own
repos so I am rehearsing code I actually write.

## Data

SQLite, one file `etude.db`, three tables.

```sql
CREATE TABLE snippets (
    id        TEXT PRIMARY KEY,
    title     TEXT NOT NULL,
    tags      TEXT NOT NULL,
    box       INTEGER NOT NULL DEFAULT 1,
    next_due  TEXT NOT NULL
);

CREATE TABLE attempts (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    snippet_id TEXT NOT NULL REFERENCES snippets(id),
    mode       TEXT NOT NULL,
    started_at TEXT NOT NULL,
    seconds    REAL NOT NULL,
    accuracy   REAL,
    passed     INTEGER NOT NULL
);

CREATE TABLE sessions (
    date                  TEXT PRIMARY KEY,
    attempts              INTEGER NOT NULL,
    blank_first_try_rate  REAL
);
```

`blank_first_try_rate` is the number I watch, week over week.

## Stack

- .NET console app.
- Spectre.Console for coloured rendering.
- Raw key input via `Console.ReadKey(intercept: true)` so each character can
  be coloured as it is typed. Handle Backspace, Enter, Tab (insert four
  spaces), and Escape (abandon the attempt, counts as a fail).
- Microsoft.Data.Sqlite for storage.
- Blank mode compiles and runs the attempt in-process with
  `Microsoft.CodeAnalysis.CSharp.Scripting`. Each test `call` is evaluated as
  a script with my code prepended. No shelling out to `dotnet build`.

## Trace mode rendering

- Reference text drawn in dim grey.
- As I type, each character is redrawn: green if it matches, red if it does
  not. Backspace restores dim grey.
- A cursor marker sits on the next expected character.
- Timer starts on the first keypress and stops when the last character is
  correct.
- Results line: accuracy (correct keystrokes / total keystrokes), seconds,
  pass or fail, new box number.

## Build order

1. **Stage 1.** Trace mode only. Load snippets from `snippets/`, pick one at
   random, render, score, print the results line. No database. Ten
   hand-written snippets.
2. **Stage 2.** SQLite. Record attempts. Leitner boxes and `next_due`. The
   daily session picks what is due. Recall mode.
3. **Stage 3.** Blank mode. Roslyn scripting, tests from the snippet header,
   first-try tracking, the `sessions` table.
4. **Stage 4.** Stats screen. Import snippets from a repo folder by selecting
   functions. A "due today" count printed on launch.

Stage 1 is a full weekend on its own. That is expected.

## Metrics

- Weekly: `blank_first_try_rate`, plotted as a simple text bar per week.
- Per snippet: current box, last three results.
- Words per minute is shown in trace mode only, in small type. It is not a
  goal.

## First ten snippets

### 001 — LINQ pipeline

```
---
id: 001
title: LINQ pipeline
tags: [linq]
modes: [trace, recall, blank]
spec: Given a list of integers, return the even ones squared, largest first.
tests:
  - call: 'string.Join(",", EvenSquares(new[] {1, 2, 3, 4}))'
    expect: '16,4'
  - call: 'string.Join(",", EvenSquares(new int[0]))'
    expect: ''
  - call: 'string.Join(",", EvenSquares(new[] {6, 2, 4}))'
    expect: '36,16,4'
---
public static IEnumerable<int> EvenSquares(IEnumerable<int> xs)
    => xs.Where(x => x % 2 == 0)
         .Select(x => x * x)
         .OrderByDescending(x => x);
```

### 002 — Async method

```
---
id: 002
title: Async delay and return
tags: [async]
modes: [trace, recall, blank]
spec: Asynchronously wait the given milliseconds, then return the input doubled.
tests:
  - call: 'DoubleLater(21, 1).Result'
    expect: '42'
  - call: 'DoubleLater(0, 1).Result'
    expect: '0'
---
public static async Task<int> DoubleLater(int x, int ms)
{
    await Task.Delay(ms);
    return x * 2;
}
```

### 003 — Generic max with constraint

```
---
id: 003
title: Generic max with constraint
tags: [generics, constraints]
modes: [trace, recall, blank]
spec: Return the larger of two values of any comparable type.
tests:
  - call: 'Max(3, 7)'
    expect: '7'
  - call: 'Max("apple", "pear")'
    expect: 'pear'
  - call: 'Max(2.5, 2.5)'
    expect: '2.5'
---
public static T Max<T>(T a, T b) where T : IComparable<T>
    => a.CompareTo(b) >= 0 ? a : b;
```

### 004 — Switch expression

```
---
id: 004
title: Switch expression on a shape
tags: [pattern-matching]
modes: [trace, recall, blank]
spec: Given a side count, return "triangle", "square", "pentagon", or "polygon".
tests:
  - call: 'Name(3)'
    expect: 'triangle'
  - call: 'Name(4)'
    expect: 'square'
  - call: 'Name(9)'
    expect: 'polygon'
---
public static string Name(int sides) => sides switch
{
    3 => "triangle",
    4 => "square",
    5 => "pentagon",
    _ => "polygon"
};
```

### 005 — Record and deconstruction

```
---
id: 005
title: Positional record
tags: [records]
modes: [trace, recall]
spec: Define a Point record with X and Y and a method returning the distance from origin.
tests:
  - call: 'new Point(3, 4).Distance()'
    expect: '5'
  - call: 'new Point(0, 0).Distance()'
    expect: '0'
---
public record Point(double X, double Y)
{
    public double Distance() => Math.Sqrt(X * X + Y * Y);
}
```

### 006 — Custom exception and guard

```
---
id: 006
title: Guarded divide
tags: [exceptions]
modes: [trace, recall, blank]
spec: Divide a by b, throwing DivideByZeroException with message "b was zero" when b is 0.
tests:
  - call: 'SafeDivide(10, 2)'
    expect: '5'
  - call: '((Func<string>)(() => { try { SafeDivide(1, 0); return "no throw"; } catch (DivideByZeroException e) { return e.Message; } }))()'
    expect: 'b was zero'
---
public static int SafeDivide(int a, int b)
{
    if (b == 0) throw new DivideByZeroException("b was zero");
    return a / b;
}
```

### 007 — Word count with a dictionary

```
---
id: 007
title: Word frequency
tags: [collections, strings]
modes: [trace, recall, blank]
spec: Count words in a string, case-insensitive, split on spaces.
tests:
  - call: 'WordCount("a b A")["a"]'
    expect: '2'
  - call: 'WordCount("x y z").Count'
    expect: '3'
  - call: 'WordCount("").Count'
    expect: '0'
---
public static Dictionary<string, int> WordCount(string text)
{
    var counts = new Dictionary<string, int>();
    foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
        var key = word.ToLowerInvariant();
        counts[key] = counts.GetValueOrDefault(key) + 1;
    }
    return counts;
}
```

### 008 — Palindrome check

```
---
id: 008
title: Palindrome
tags: [strings, two-pointers]
modes: [trace, recall, blank]
spec: Return true if the string reads the same backwards, ignoring case.
tests:
  - call: 'IsPalindrome("Level")'
    expect: 'True'
  - call: 'IsPalindrome("hello")'
    expect: 'False'
  - call: 'IsPalindrome("")'
    expect: 'True'
---
public static bool IsPalindrome(string s)
{
    int i = 0, j = s.Length - 1;
    while (i < j)
    {
        if (char.ToLowerInvariant(s[i]) != char.ToLowerInvariant(s[j])) return false;
        i++; j--;
    }
    return true;
}
```

### 009 — Iterator with yield

```
---
id: 009
title: Fibonacci iterator
tags: [iterators, yield]
modes: [trace, recall, blank]
spec: Yield the first n Fibonacci numbers starting from 0.
tests:
  - call: 'string.Join(",", Fib(5))'
    expect: '0,1,1,2,3'
  - call: 'string.Join(",", Fib(1))'
    expect: '0'
  - call: 'string.Join(",", Fib(0))'
    expect: ''
---
public static IEnumerable<long> Fib(int n)
{
    long a = 0, b = 1;
    for (int i = 0; i < n; i++)
    {
        yield return a;
        (a, b) = (b, a + b);
    }
}
```

### 010 — Binary search

```
---
id: 010
title: Binary search
tags: [algorithms, search]
modes: [trace, recall, blank]
spec: Return the index of target in a sorted array, or -1 if absent.
tests:
  - call: 'Search(new[] {1, 3, 5, 7, 9}, 7)'
    expect: '3'
  - call: 'Search(new[] {1, 3, 5, 7, 9}, 4)'
    expect: '-1'
  - call: 'Search(new int[0], 1)'
    expect: '-1'
---
public static int Search(int[] xs, int target)
{
    int lo = 0, hi = xs.Length - 1;
    while (lo <= hi)
    {
        int mid = lo + (hi - lo) / 2;
        if (xs[mid] == target) return mid;
        if (xs[mid] < target) lo = mid + 1;
        else hi = mid - 1;
    }
    return -1;
}
```

## Repo layout

```
etude/
  CLAUDE.md
  SPEC.md
  PROGRESS.md
  snippets/
    001-linq-pipeline.md
    ...
  src/
    Etude/
      Etude.csproj
      Program.cs
  tests/
```
