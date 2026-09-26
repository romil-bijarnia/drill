---
id: asm-026
title: Trailing zeros
lang: asm
tags: [bits, registers]
modes: [trace, recall, blank]
spec: long ctz(long n) returns the number of trailing zero bits of n, 64 for 0: there is no ctz instruction, so rbit reverses the bits and clz counts the zeros from the top.
decl: 'long ctz(long);'
tests:
  - call: 'ctz(8)'
    expect: '3'
  - call: 'ctz(1)'
    expect: '0'
  - call: 'ctz(96)'
    expect: '5'
  - call: 'ctz(-4)'
    expect: '2'
  - call: 'ctz(0)'
    expect: '64'
---
.text
.globl _ctz
.p2align 2
_ctz:
    rbit x0, x0
    clz x0, x0
    ret
