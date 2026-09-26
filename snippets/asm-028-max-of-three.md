---
id: asm-028
title: Max of three
lang: asm
tags: [branches, arithmetic]
modes: [trace, recall, blank]
spec: long max3(long a, long b, long c) returns the largest of the three with cmp and csel twice and no branch.
decl: 'long max3(long, long, long);'
tests:
  - call: 'max3(1, 2, 3)'
    expect: '3'
  - call: 'max3(9, 2, 3)'
    expect: '9'
  - call: 'max3(1, 7, 3)'
    expect: '7'
  - call: 'max3(-5, -5, -9)'
    expect: '-5'
---
.text
.globl _max3
.p2align 2
_max3:
    cmp x0, x1
    csel x0, x0, x1, gt
    cmp x0, x2
    csel x0, x0, x2, gt
    ret
