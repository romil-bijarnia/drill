---
id: machine-05-assembler
title: Machine 5 — ARM64 assembler
status: later
stack: [python]
---
An assembler for a subset of real ARM64, in Python: it turns `add x0, x1, x2` into the
exact 32-bit encoding from the Arm Architecture Reference Manual, resolves labels, and
writes an object file that the system linker accepts. Every encoding is checked against
what clang produces for the same line. Done means every asm snippet in this repo
assembles byte-for-byte identical to clang.

## Steps
- [ ] Parse one line into mnemonic and operands (registers, immediates, labels) and print the tokens; reject anything else with a line number
- [ ] Encode ADD and SUB (shifted register, 64-bit) from the bit layout in the manual; check the four bytes against `clang -c` plus `otool -t` for the same instruction
- [ ] ADD and SUB immediate, MOV as ORR with xzr, MOVZ and MOVK for loading constants
- [ ] AND, ORR, EOR register with the optional shift operand; LSL, LSR, ASR as their aliases
- [ ] LDR and STR with an unsigned immediate offset, LDRB and STRB, and the pre- and post-indexed forms
- [ ] B, B.cond, CBZ, CBNZ, BL and RET; labels resolved in a second pass with PC-relative offsets in words
- [ ] Write a minimal Mach-O object (header, one segment, one __text section, a symbol table with one global symbol) that `ld` accepts, and link a program whose every instruction came from your encoder
- [ ] Assemble every asm snippet in this repo and byte-compare with clang; fix the differences; README; push
