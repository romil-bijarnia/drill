---
id: asm-016
title: Absolute difference
lang: asm
tags: [arithmetic, branches]
modes: [trace, recall, blank]
spec: long abs_diff(long a, long b) returns |a - b|: subs sets the flags on the difference and cneg negates it when it came out negative (mi).
decl: 'long abs_diff(long, long);'
tests:
  - call: 'abs_diff(3, 10)'
    expect: '7'
  - call: 'abs_diff(10, 3)'
    expect: '7'
  - call: 'abs_diff(-4, 6)'
    expect: '10'
  - call: 'abs_diff(5, 5)'
    expect: '0'
---
.text
.globl _abs_diff
.p2align 2
_abs_diff:
    subs x0, x0, x1
    cneg x0, x0, mi
    ret
