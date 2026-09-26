---
id: machine-06-compiler
title: Machine 6 — a compiler that emits ARM64
status: later
stack: [c]
---
A compiler, in C, for a small language with integers, variables, if, while and
functions, that emits ARM64 assembly text which cc assembles and links. No parser
generators, no LLVM: a hand-written tokenizer, a recursive-descent parser, an AST, and a
code generator that follows the AAPCS64 calling convention. Done means twenty test
programs compile and produce the expected output from a script.

## Steps
- [ ] Compile the program `42`: emit a .s file with a _main that returns 42; build it with cc and check `echo $?`
- [ ] Integer expressions with + - * / and parentheses: tokenizer, recursive-descent parser producing an AST, code generator using the stack for temporaries
- [ ] Variables: `let x = 3;` in stack slots inside the frame; `print x;` calls printf from a small runtime .c file
- [ ] `if` and `else` with comparisons: cmp, b.cond, unique labels
- [ ] `while` loops: a compiled program computes fib(30) and matches Python
- [ ] Functions with parameters and returns following AAPCS64 (x0–x7, stp x29/x30, 16-byte aligned stack); recursive fib compiles and runs
- [ ] Keep expression temporaries in x9–x15 and spill only when they run out; time fib(35) before and after
- [ ] Twenty test programs with expected outputs run by a script; README with the grammar and one example .s; push
