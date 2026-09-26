---
id: asm-029
title: Three-way compare
lang: asm
tags: [branches, arithmetic]
modes: [trace, recall, blank]
spec: long compare(long a, long b) returns -1, 0 or 1 as a is less than, equal to or greater than b: after one cmp, csetm gives -1 for lt and cinc adds one for gt, so LONG_MIN against 1 cannot overflow the way a - b would.
decl: 'long compare(long, long);'
tests:
  - call: 'compare(1, 2)'
    expect: '-1'
  - call: 'compare(2, 1)'
    expect: '1'
  - call: 'compare(5, 5)'
    expect: '0'
  - call: 'compare(LONG_MIN, 1)'
    expect: '-1'
---
.text
.globl _compare
.p2align 2
_compare:
    cmp x0, x1
    csetm x2, lt
    cinc x0, x2, gt
    ret
