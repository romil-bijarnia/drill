---
id: asm-040
title: Recursive factorial
lang: asm
tags: [recursion, calls, stack]
modes: [trace, recall, blank]
spec: long fact_rec(long n) returns n! by recursion: n <= 1 returns 1 with no frame at all; otherwise save x29, x30 and x19, keep n in x19 across bl _fact_rec, and multiply the returned value by it.
decl: 'long fact_rec(long);'
tests:
  - call: 'fact_rec(5)'
    expect: '120'
  - call: 'fact_rec(1)'
    expect: '1'
  - call: 'fact_rec(0)'
    expect: '1'
  - call: 'fact_rec(20)'
    expect: '2432902008176640000'
---
.text
.globl _fact_rec
.p2align 2
_fact_rec:
    cmp x0, #1
    b.le 1f
    stp x29, x30, [sp, #-32]!
    mov x29, sp
    str x19, [sp, #16]
    mov x19, x0
    sub x0, x0, #1
    bl _fact_rec
    mul x0, x0, x19
    ldr x19, [sp, #16]
    ldp x29, x30, [sp], #32
    ret
1:
    mov x0, #1
    ret
