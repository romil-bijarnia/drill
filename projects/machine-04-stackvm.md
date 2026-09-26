---
id: machine-04-stackvm
title: Machine 4 — stack VM and assembler
status: later
stack: [c]
---
A small virtual machine in C with its own bytecode, plus an assembler that turns text
programs with labels into that bytecode. Done means a recursive factorial written in
your assembly language runs on your VM, and the README has the full instruction table.

## Steps
- [ ] Define the bytecode: PUSH n, ADD, SUB, MUL, DIV, PRINT, HALT; a hand-written byte array that prints 42 runs through a fetch loop with a switch
- [ ] A stack with bounds checks: underflow and overflow stop the machine with a message and a nonzero exit code
- [ ] DUP, SWAP, DROP, and EQ, LT, GT pushing 0 or 1
- [ ] JMP and JZ with absolute addresses; a program that counts down from 10, printing each number
- [ ] LOAD and STORE to 256 memory slots; a program that computes fib(20) with a loop
- [ ] The assembler: reads lines like `push 3` and `jz end` with labels, resolves them in a second pass, and writes the bytes; the fib program now lives in a .vm text file
- [ ] CALL and RET with a separate return stack; a recursive factorial in .vm
- [ ] README with the instruction table and three example programs; push
