# Drill

Writing code from a blank file, all the way down the stack: Python, C#, C and ARM64
assembly. One rep after another for as long as you want, the same problem taken from
Python down to assembly, quick-fire bits and bytes, real projects with timed work blocks,
and one number that matters.

## Snippets: four ways to practise each one

| Mode    | What you see                             | What you do                                       | Pass                                              |
|---------|------------------------------------------|---------------------------------------------------|---------------------------------------------------|
| Recall  | The code, for as long as you need        | Hide it, write it yourself; Ctrl+D runs the tests | Every test passes (exact match if there are none) |
| Blank   | A one-line spec and the tests            | Write it; Ctrl+D compiles and runs                | Every test passes                                 |
| Predict | The code and each test call              | Type what the call returns before anything runs   | Every call predicted                              |
| Trace   | The code, dim, on screen                 | Type over it; every key goes green/red            | 97 % of keystrokes correct                        |

Each snippet sits in a box from 1 to 5. Box 1, where every snippet starts and where a miss
sends it back, is recall; boxes 2 to 5 are blank. A pass moves it up a box and it comes
back after 2, 4, 8 or 16 days. `drill` serves one rep after another in that order: whatever
is ready first, then the weakest, then the least recent, with no count to clear; q stops
whenever you like. Trace and predict are never scheduled and move no boxes: `drill trace
asm` when the syntax is new to you, `drill predict c` to train the model in your head
before your fingers. The scoreboard is the blank-mode first-try pass rate, week over week.

Recall is not memorising. Read the reference until you understand it, hide it, and write
a version that passes the tests; different names or a different approach are fine. After
a miss, r shows the reference again for another go, and only the first go counts as a
first try.

Trace grades code, not spacing: indentation and the spaces between tokens fill in as you
type the next character, stray spaces are ignored, and only Enter is required to end a
line. A wrong character shows red until you Backspace over it.

Recall and blank use a small built-in editor with nothing in it to help you: Ctrl+D
submits, Esc gives up, Tab indents, Enter keeps the indentation (after `{`, or `:` in
Python, it adds a level). `DRILL_EDITOR="nano"` or `"code --wait"` if you must use an
external editor; leave autocomplete off.

## Down the stack

Snippets that solve the same problem in different languages share a `family:` name.
`drill families` shows every such problem and how far down you have taken it; `drill down`
serves one problem in Python, then C#, then C, then ARM64, opening the next language only
when the one above it passes, and moves to another problem when that one is done all the
way down. `drill down strlen` sticks to one problem.

`drill compile` goes the other way: it shows a C snippet next to what clang emits for it
at -O1, and then asks for your own ARM64 for the same tests, with the prototype taken from
the C source. Any C snippet with tests can be taken down this way.

## Bits

`drill bits` is quick-fire questions on what sits under the code: hex and binary, two's
complement, shifts and rotates, masks, byte order, popcount and friends, and what one
ARM64 instruction does to a register (`add`, `lsr`, `csel`, `ubfx`, `ldrb`, `movk`, the
flags after `cmp`, and so on). They are generated as you go, so they never run out. `drill
bits arm` picks a topic: hex, twos, shift, mask, endian, pop, arm. Accuracy and speed per
topic show in `drill stats`.

## Commands

```
drill [lang]                 one rep after another, q stops whenever you like
drill recall [id|lang]       read it, hide it, write it; the tests decide
drill blank [id|lang]        the spec and the tests only
drill predict [id|lang]      say what each test call returns before it runs
drill trace [id|lang]        type over the reference
drill down [problem]         one problem from Python down to ARM64
drill families               the problems that exist in several languages
drill compile [c-id]         clang's ARM64 for a C snippet, then your own
drill bits [topic]           quick-fire hex, twos, shift, mask, endian, pop, arm
drill interview [lang|id] [minutes]   one blank problem, 45 min by default, one submission

drill projects               real builds with steps: progress and hours
drill project <id>           one project's goal, steps and time
drill project new <id> <title>
drill work [id] [step]       timed block on the next open step; Enter marks it done

drill list [lang] [tag]      every snippet: box, next date, last three results
drill stats                  streak, boxes, predict and bits accuracy, weekly blank first-try rate, project hours
drill verify [lang|id]       reference code passes its own tests
drill new <lang-id> <title>  a new snippet file to fill in (cs-, py-, c-, asm-)
drill import <file> [tags]   your own functions (.cs .py .c .s) as trace/recall snippets
drill where                  paths, editor and toolchains in use
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

`drill work devdeakin-site` shows the next open step and starts a clock; you build in your
real editor and repo; Enter marks the step done (the file is updated in place) and offers
the next one, p pauses, q stops. Hours per project show in `drill projects` and `drill stats`.
Projects with `status: later` wait on one line until you set them active.

Two ladders ship as projects. The machine ladder goes down to the metal and is the one
that is active: a hexdump tool, a Mach-O reader, your own malloc, a stack VM with an
assembler, an ARM64 assembler, a compiler that emits ARM64, and bare metal on QEMU. The
coaching ladder (calculator, file tools, data structures, algorithms, small systems) is
parked as later. `drill interview` is the rung after any of them.

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

`spec` is all blank mode shows (plus `decl` for assembly). `family: gcd` links snippets
that solve the same problem in different languages for `drill down`. Each `call` is an expression in
the test language evaluated with your code in scope; it passes when the printed result
equals `expect`. Snippets without tests are trace and recall only. The bank ships with 56 C#, 51 Python, 62 C and 52 assembly snippets; `drill verify` before you practise new ones.

## Data

`drill.db` (SQLite, git-ignored) in the repo root: `snippets` (box, next_due), `attempts`
(mode, seconds, accuracy, passed, first_try), `sessions` (date, attempts,
blank_first_try_rate), `work` (project, step, seconds, completed), `bits` (kind, seconds,
correct, question, answer, given). The root is
`DRILL_HOME`, else the nearest parent directory with a `snippets/` folder, else
`~/Documents/drill`.

## Build

.NET 10 SDK; `cc` (Xcode command line tools) for C and assembly; `python3` for Python.
`dotnet build src/Drill`, `dotnet test tests/Drill.Tests`. Install as a command with
`Tools/install.sh` (publishes to `~/.local/share/drill` and links `drill` into `~/.local/bin`).
