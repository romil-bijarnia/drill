---
id: asm-041
title: Recursive fibonacci
lang: asm
tags: [recursion, calls, stack]
modes: [trace, recall, blank]
spec: long fib_rec(long n) returns F(n) by the two-call recursion (F0 = 0, F1 = 1): n < 2 returns n as it is; otherwise x19 keeps n and x20 keeps fib(n - 1) across the calls, saved together with one stp and restored with one ldp.
decl: 'long fib_rec(long);'
tests:
  - call: 'fib_rec(10)'
    expect: '55'
  - call: 'fib_rec(1)'
    expect: '1'
  - call: 'fib_rec(0)'
    expect: '0'
  - call: 'fib_rec(20)'
    expect: '6765'
---
.text
.globl _fib_rec
.p2align 2
_fib_rec:
    cmp x0, #2
    b.lt 1f
    stp x29, x30, [sp, #-32]!
    mov x29, sp
    stp x19, x20, [sp, #16]
    mov x19, x0
    sub x0, x0, #1
    bl _fib_rec
    mov x20, x0
    sub x0, x19, #2
    bl _fib_rec
    add x0, x0, x20
    ldp x19, x20, [sp, #16]
    ldp x29, x30, [sp], #32
1:
    ret
