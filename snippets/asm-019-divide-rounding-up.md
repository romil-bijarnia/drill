---
id: asm-019
title: Divide rounding up
lang: asm
tags: [arithmetic, branches]
modes: [trace, recall, blank]
spec: long div_ceil(long a, long b) returns a / b rounded up, for a >= 0 and b > 0: udiv for the quotient, msub for the remainder, and cinc adds one when the remainder is not zero.
decl: 'long div_ceil(long, long);'
tests:
  - call: 'div_ceil(7, 2)'
    expect: '4'
  - call: 'div_ceil(8, 2)'
    expect: '4'
  - call: 'div_ceil(0, 5)'
    expect: '0'
  - call: 'div_ceil(1, 5)'
    expect: '1'
---
.text
.globl _div_ceil
.p2align 2
_div_ceil:
    udiv x2, x0, x1
    msub x3, x2, x1, x0
    cmp x3, #0
    cinc x0, x2, ne
    ret
