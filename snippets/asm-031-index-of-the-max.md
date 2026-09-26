---
id: asm-031
title: Index of the max
lang: asm
tags: [loops, memory, addressing]
modes: [trace, recall, blank]
spec: long argmax(const long *xs, long n) returns the index of the largest of n >= 1 values, the first one on ties: the register-offset load ldr x5, [x0, x4, lsl #3] reads xs[x4], and two csel with the same condition update the best value and its index together.
decl: 'long argmax(const long *, long);'
tests:
  - call: 'argmax((long[]){3, 9, 4}, 3)'
    expect: '1'
  - call: 'argmax((long[]){5, 5, 5}, 3)'
    expect: '0'
  - call: 'argmax((long[]){-1, -7, -3}, 3)'
    expect: '0'
  - call: 'argmax((long[]){1, 2, 3, 4}, 4)'
    expect: '3'
  - call: 'argmax((long[]){2}, 1)'
    expect: '0'
---
.text
.globl _argmax
.p2align 2
_argmax:
    ldr x2, [x0]
    mov x3, #0
    mov x4, #1
1:
    cmp x4, x1
    b.ge 2f
    ldr x5, [x0, x4, lsl #3]
    cmp x5, x2
    csel x2, x5, x2, gt
    csel x3, x4, x3, gt
    add x4, x4, #1
    b 1b
2:
    mov x0, x3
    ret
