# Reps

A gym for writing code from a blank file, in C#, Python, C and ARM64 assembly. One rep
after another for as long as you want, real projects with timed work blocks, and one
number that matters.

## Snippets: three ways to practise each one

| Mode   | What you see                             | What you do                                  | Pass                                                |
|--------|------------------------------------------|----------------------------------------------|-----------------------------------------------------|
| Recall | The code, for as long as you need        | Hide it, write it yourself; Ctrl+D runs the tests | Every test passes (exact match if there are none) |
| Blank  | A one-line spec and the tests            | Write it; Ctrl+D compiles and runs           | Every test passes                                   |
| Trace  | The code, dim, on screen                 | Type over it; every key goes green/red       | 97 % of keystrokes correct                          |

Each snippet sits in a box from 1 to 5. Box 1, where every snippet starts and where a miss
sends it back, is recall; boxes 2 to 5 are blank. A pass moves it up a box and it comes
back after 2, 4, 8 or 16 days. `reps` serves one rep after another in that order: whatever
is ready first, then the weakest, then the least recent, with no count to clear; q stops
whenever you like. Trace is never scheduled: `reps trace asm` when the syntax is new to
you. The scoreboard is the blank-mode first-try pass rate, week over week.

Recall is not memorising. Read the reference until you understand it, hide it, and write
a version that passes the tests; different names or a different approach are fine. After
a miss, r shows the reference again for another go, and only the first go counts as a
first try.

Trace grades code, not spacing: indentation and the spaces between tokens fill in as you
type the next character, stray spaces are ignored, and only Enter is required to end a
line. A wrong character shows red until you Backspace over it.

Recall and blank use a small built-in editor with nothing in it to help you: Ctrl+D
submits, Esc gives up, Tab indents, Enter keeps the indentation (after `{`, or `:` in
Python, it adds a level). `REPS_EDITOR="nano"` or `"code --wait"` if you must use an
external editor; leave autocomplete off.

## Commands

```
reps [lang]                 one rep after another, q stops whenever you like
reps recall [id|lang]       read it, hide it, write it; the tests decide
reps blank [id|lang]        the spec and the tests only
reps trace [id|lang]        type over the reference
reps interview [lang|id] [minutes]   one blank problem, 45 min by default, one submission

reps projects               real builds with steps: progress and hours
reps project <id>           one project's goal, steps and time
reps project new <id> <title>
reps work [id] [step]       timed block on the next open step; Enter marks it done

reps list [lang] [tag]      every snippet: box, next date, last three results
reps stats                  streak, boxes per language, weekly blank first-try rate, project hours
reps verify [lang|id]       reference code passes its own tests
reps new <lang-id> <title>  a new snippet file to fill in (cs-, py-, c-, asm-)
reps import <file> [tags]   your own functions (.cs .py .c .s) as trace/recall snippets
reps where                  paths, editor and toolchains in use
```

`lang` is `cs`, `py`, `c` or `asm` and can be given wherever an id can.

## Languages

C# runs in-process through Roslyn scripting. Python runs `python3` on a script that
prints one result per test; a `raises(fn)` helper is available in tests and returns the
exception type name. C is compiled with `cc -std=c11` into a harness that prints each test
expression through a `_Generic` dispatcher, so tests need no format strings (`_Bool`
prints `true`/`false`, doubles use `%g`). Assembly is AArch64 for Apple Silicon macOS:
your `.s` file is assembled and linked against the same C harness, which calls your
routine through the prototype in the snippet's `decl` line (arguments in x0–x7, result
in x0, symbols start with an underscore). A segfault shows up as a failed test, not a
crashed trainer.

## Projects

Snippets train the hands; projects train finishing. A project is one Markdown file in
`projects/` with a goal and a checklist:

```
---
id: devdeakin-site
title: DEV@Deakin site
status: active
stack: [react, firebase]
---
What it is, who it is for, what done looks like.

## Steps
- [ ] Repo, deploy skeleton, one page live
- [x] Login page
- [ ] Post feed
```

`reps work devdeakin-site` shows the next open step and starts a clock; you build in your
real editor and repo; Enter marks the step done (the file is updated in place) and offers
the next one, p pauses, q stops. Hours per project show in `reps projects` and `reps stats`.
The five rungs of the coaching ladder ship as projects: calculator, file tools, data
structures, algorithms, small systems. `reps interview` is the sixth rung.

## Snippet files

One Markdown file each in `snippets/`:

```
---
id: asm-004
title: Sum an array
lang: asm
tags: [loops, memory]
modes: [trace, recall, blank]
spec: long sum_array(const long *xs, long n): loop with a post-indexed ldr.
decl: 'long sum_array(const long *, long);'
tests:
  - call: 'sum_array((long[]){1, 2, 3, 4}, 4)'
    expect: '10'
---
.text
.globl _sum_array
...
```

`spec` is all blank mode shows (plus `decl` for assembly). Each `call` is an expression in
the test language evaluated with your code in scope; it passes when the printed result
equals `expect`. Snippets without tests are trace and recall only. The bank ships with 45
C#, 40 Python, 24 C and 12 assembly snippets; `reps verify` before you practise new ones.

## Data

`reps.db` (SQLite, git-ignored) in the repo root: `snippets` (box, next_due), `attempts`
(mode, seconds, accuracy, passed, first_try), `sessions` (date, attempts,
blank_first_try_rate), `work` (project, step, seconds, completed). The root is
`REPS_HOME`, else the nearest parent directory with a `snippets/` folder, else
`~/Documents/reps`.

## Build

.NET 10 SDK; `cc` (Xcode command line tools) for C and assembly; `python3` for Python.
`dotnet build src/Reps`, `dotnet test tests/Reps.Tests`. Install as a command with
`Tools/install.sh` (publishes to `~/.local/share/reps` and links `reps` into `~/.local/bin`).
