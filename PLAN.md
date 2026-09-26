# The plan

The aim: to be able to write anything from nothing, alone, all the way down to the machine.
Not "know assembly": have written, by hand and from an empty file, a hexdump, a binary
parser, a memory allocator, a virtual machine, an assembler, a compiler that emits ARM64,
and a kernel stub that boots on bare metal. Nobody gets to "better than everyone" by
aiming at it; they get there by being the person who can build the thing in front of them
every single time, for years. This ladder is that, in order. Two hours a day, every day.

Hour one is always `drill`; what it serves changes by phase. Hour two is always the active
project, one step. Sundays: `drill stats`, and the number is compared only with the week
before. Stuck for twenty minutes: one hint from the coach, never more. No AI writes a line.

## Phase 1 · C and the bytes · 28 Sep to 25 Oct 2026

Hour one: `drill c` until the clock, then five minutes of `drill bits`.
Hour two: machine-01-hexdump, then machine-02-macho.
Done when: hexdump is byte-identical to `hexdump -C` on every file; the Mach-O reader
agrees with `otool -l` on /bin/ls; the C blank first-try rate is higher than in week one.

## Phase 2 · Assembly · 26 Oct to 22 Nov 2026

Hour one: `drill down` until every family is done to ARM64, then `drill compile` and
`drill asm`; `drill bits arm` daily.
Hour two: machine-03-malloc, then machine-04-stackvm.
Done when: all families show ✓ in the ARM64 column; malloc survives the stress test; the
VM runs a recursive factorial written in its own assembly language.

## Phase 3 · The machine from the inside · 23 Nov 2026 to 3 Jan 2027

Hour one: `drill asm` and `drill compile`, with `drill predict c` twice a week.
Hour two: machine-05-assembler (instruction encoding is where binary stops being a word),
then machine-06-compiler.
Done when: the assembler is byte-identical to clang on every asm snippet in this repo; the
compiler passes twenty test programs including recursive fib.

## Phase 4 · Bare metal and real code · 4 Jan to 14 Feb 2027

Hour one: `drill` (everything), `drill interview` once a week.
Hour two: machine-07-baremetal; then read real code and write down what it does: the
string functions in musl, the boot path in xv6, the disassembly of your own binaries.
Done when: a tick prints every second on QEMU from your own vector table and timer handler.

## Phase 5 · The top of the stack · from 15 Feb 2027

Python by hand: Karpathy's Zero to Hero and nanoGPT, typed and understood, no copying;
`drill py` and `drill down` keep the lower floors warm. This is the floor OpenAI lives on,
and by then the floors under it are yours.

## Reading, if there is a third hour

Free, one per phase, read with the terminal open: Beej's Guide to C (phase 1); the Arm
Architecture Reference Manual, chapters on the instruction set, and Apple's ARM64 ABI
notes (phase 2); Crafting Interpreters (phase 3); the xv6 book (phase 4).

## Review

27 Oct 2026: first review. What passed cold, what did not, hours on the projects, and what
to add. Weight goes up only when the two hours have been done every day.
