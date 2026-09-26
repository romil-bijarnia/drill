---
id: asm-024
title: Is a power of two
lang: asm
tags: [bits, branches]
modes: [trace, recall, blank]
spec: long is_pow2(long n) returns 1 when exactly one bit of n is set and 0 otherwise: n & (n - 1) clears the lowest set bit, tst checks whether anything is left, and cbz sends zero straight out.
decl: 'long is_pow2(long);'
tests:
  - call: 'is_pow2(8)'
    expect: '1'
  - call: 'is_pow2(6)'
    expect: '0'
  - call: 'is_pow2(1)'
    expect: '1'
  - call: 'is_pow2(0)'
    expect: '0'
---
.text
.globl _is_pow2
.p2align 2
_is_pow2:
    cbz x0, 1f
    sub x1, x0, #1
    tst x0, x1
    cset x0, eq
1:
    ret
