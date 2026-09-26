---
id: asm-002
title: Maximum of two
lang: asm
tags: [compare]
modes: [trace, recall, blank]
spec: long max64(long a, long b) with cmp and csel. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long max64(long, long);'
tests:
  - call: 'max64(3, 7)'
    expect: '7'
  - call: 'max64(-2, -9)'
    expect: '-2'
---
.text
.globl _max64
.p2align 2
_max64:
    cmp x0, x1
    csel x0, x0, x1, gt
    ret
