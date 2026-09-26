---
id: asm-049
title: Maximum of an array
lang: asm
tags: [loops, memory, compare]
family: max
modes: [trace, recall, blank]
spec: long max_of(const long *xs, long n) returns the largest of n >= 1 values; post-indexed ldr through the array, keep the best with cmp and csel. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long max_of(const long *, long);'
tests:
  - call: 'max_of((long[]){3, 9, 2}, 3)'
    expect: '9'
  - call: 'max_of((long[]){-5}, 1)'
    expect: '-5'
  - call: 'max_of((long[]){-3, -1, -2}, 3)'
    expect: '-1'
  - call: 'max_of((long[]){7, 7, 1}, 3)'
    expect: '7'
---
.text
.globl _max_of
.p2align 2
_max_of:
    ldr x2, [x0], #8
    sub x1, x1, #1
1:
    cbz x1, 2f
    ldr x3, [x0], #8
    cmp x3, x2
    csel x2, x3, x2, gt
    sub x1, x1, #1
    b 1b
2:
    mov x0, x2
    ret
