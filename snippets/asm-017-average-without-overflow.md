---
id: asm-017
title: Average without overflow
lang: asm
tags: [arithmetic, bits]
modes: [trace, recall, blank]
spec: long average(long a, long b) returns floor((a + b) / 2) without ever overflowing, as (a & b) + ((a ^ b) >> 1); the shift rides on add as a shifted operand.
decl: 'long average(long, long);'
tests:
  - call: 'average(4, 8)'
    expect: '6'
  - call: 'average(3, 4)'
    expect: '3'
  - call: 'average(-3, -4)'
    expect: '-4'
  - call: 'average(LONG_MAX, LONG_MAX)'
    expect: '9223372036854775807'
  - call: 'average(-3, 4)'
    expect: '0'
---
.text
.globl _average
.p2align 2
_average:
    eor x2, x0, x1
    and x0, x0, x1
    add x0, x0, x2, asr #1
    ret
