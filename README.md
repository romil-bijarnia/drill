# Reps

A gym for writing C# from a blank file. Short daily sets; one number that matters.

Three ways to practise a snippet, each harder than the last:

| Mode   | What you see                        | What you do                            | Pass                              |
|--------|-------------------------------------|----------------------------------------|-----------------------------------|
| Trace  | The code, dim, on screen            | Type over it; every key goes green/red | 97 % of keystrokes correct        |
| Recall | The code for a few seconds, then nothing | Type it from memory                | Identical once whitespace is ignored |
| Blank  | A one-line spec and the tests       | Write it; Ctrl+D compiles and runs     | Every test passes                 |

Each snippet lives in a Leitner box from 1 to 5. Box 1 is practised in trace, box 2 in
recall, boxes 3 to 5 in blank. A pass moves it up a box and it comes back in 1, 2, 4, 8
or 16 days; a fail drops it to box 1. `reps` runs everything due today, at most fifteen.
The scoreboard is the blank-mode first-try pass rate, week over week.

## Use

```
reps                  today's session
reps trace 015        one rep in a chosen mode (id optional: picks at random)
reps recall 015
reps blank 015
reps list [tag]       every snippet: box, due date, last three results
reps stats            streak, boxes, weekly blank first-try rate
reps verify           every snippet's reference code passes its own tests
reps new 046 Title    a new snippet file to fill in
reps import File.cs   your own static methods, as trace/recall snippets
```

Recall and blank use a small built-in editor with nothing to help you: Ctrl+D submits,
Esc gives up, Tab indents, Enter keeps the indentation. If you insist on an external
editor, `REPS_EDITOR="nano"` or `REPS_EDITOR="code --wait"`; leave autocomplete off.

## Snippets

One Markdown file each in `snippets/`: a small header, then the reference code.

```
---
id: 015
title: Greatest common divisor
tags: [algorithms, recursion]
modes: [trace, recall, blank]
spec: Greatest common divisor of two non-negative integers, recursively.
tests:
  - call: 'Gcd(12, 18)'
    expect: '6'
---
public static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
```

`spec` is all blank mode shows. Each `call` is a C# expression evaluated with your code
in scope (Roslyn scripting, in-process); it passes when the result's invariant
`ToString()` equals `expect`. Snippets without tests are trace and recall only. Forty-five
ship in the bank; add your own, and `reps verify` before you practise them.

## Data

`reps.db` (SQLite, git-ignored) in the repo root: `snippets` (box, next_due), `attempts`
(mode, seconds, accuracy, passed, first_try), `sessions` (date, attempts,
blank_first_try_rate). The root is `REPS_HOME`, else the nearest parent directory with a
`snippets/` folder, else `~/Documents/reps`.

## Build

.NET 10 SDK. `dotnet build src/Reps`, `dotnet test tests/Reps.Tests`. Install as a
command with `Tools/install.sh` (publishes to `~/.local/share/reps` and links `reps`
into `~/.local/bin`).
