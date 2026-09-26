---
id: asm-010
title: Swap through pointers
lang: asm
tags: [memory]
modes: [trace, recall, blank]
spec: void swap_longs(long *a, long *b) exchanges the two values with ldr and str. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'void swap_longs(long *, long *);'
tests:
  - call: '({ long a = 1, b = 2; swap_longs(&a, &b); a * 10 + b; })'
    expect: '21'
---
.text
.globl _swap_longs
.p2align 2
_swap_longs:
    ldr x2, [x0]
    ldr x3, [x1]
    str x3, [x0]
    str x2, [x1]
    ret
