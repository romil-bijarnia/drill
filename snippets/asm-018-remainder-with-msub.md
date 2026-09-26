---
id: asm-018
title: Remainder with msub
lang: asm
tags: [arithmetic, registers]
modes: [trace, recall, blank]
spec: long rem64(long a, long b) returns a % b (b != 0) with C's sign rule: sdiv truncates the quotient, then msub subtracts quotient * b from a.
decl: 'long rem64(long, long);'
tests:
  - call: 'rem64(17, 5)'
    expect: '2'
  - call: 'rem64(-17, 5)'
    expect: '-2'
  - call: 'rem64(10, 5)'
    expect: '0'
  - call: 'rem64(3, 7)'
    expect: '3'
---
.text
.globl _rem64
.p2align 2
_rem64:
    sdiv x2, x0, x1
    msub x0, x2, x1, x0
    ret
