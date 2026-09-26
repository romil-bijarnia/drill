---
id: asm-039
title: Sum of two lengths
lang: asm
tags: [calls, stack, registers]
modes: [trace, recall, blank]
spec: long len_sum(const char *a, const char *b) returns strlen(a) + strlen(b): b and the first length have to survive a call, so they live in the callee-saved x19 and x20, which are themselves saved above the frame record in a 32-byte frame and restored before ret.
decl: 'long len_sum(const char *, const char *);'
tests:
  - call: 'len_sum("abc", "de")'
    expect: '5'
  - call: 'len_sum("x", "")'
    expect: '1'
  - call: 'len_sum("", "")'
    expect: '0'
---
.text
.globl _len_sum
.p2align 2
_len_sum:
    stp x29, x30, [sp, #-32]!
    mov x29, sp
    stp x19, x20, [sp, #16]
    mov x19, x1
    bl _strlen
    mov x20, x0
    mov x0, x19
    bl _strlen
    add x0, x0, x20
    ldp x19, x20, [sp, #16]
    ldp x29, x30, [sp], #32
    ret
