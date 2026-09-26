---
id: asm-015
title: Clamp to a range
lang: asm
tags: [arithmetic, branches]
modes: [trace, recall, blank]
spec: long clamp(long x, long lo, long hi) returns x limited to the range lo..hi with two cmp and csel pairs and no branch.
decl: 'long clamp(long, long, long);'
tests:
  - call: 'clamp(5, 0, 10)'
    expect: '5'
  - call: 'clamp(-3, 0, 10)'
    expect: '0'
  - call: 'clamp(42, 0, 10)'
    expect: '10'
  - call: 'clamp(10, 10, 10)'
    expect: '10'
---
.text
.globl _clamp
.p2align 2
_clamp:
    cmp x0, x1
    csel x0, x1, x0, lt
    cmp x0, x2
    csel x0, x2, x0, gt
    ret
