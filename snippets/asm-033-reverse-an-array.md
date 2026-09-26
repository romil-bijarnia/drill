---
id: asm-033
title: Reverse an array
lang: asm
tags: [loops, memory, addressing]
modes: [trace, recall, blank]
spec: void reverse_longs(long *xs, long n) reverses the n values in place: x1 becomes a pointer one past the end, and while the two pointers are more than one element apart, ldr x3, [x1, #-8]! pre-decrements the back pointer and str x4, [x0], #8 post-increments the front one.
decl: 'void reverse_longs(long *, long);'
tests:
  - call: '({ long a[] = {1, 2, 3, 4}; reverse_longs(a, 4); a[0] * 1000 + a[1] * 100 + a[2] * 10 + a[3]; })'
    expect: '4321'
  - call: '({ long a[] = {1, 2, 3}; reverse_longs(a, 3); a[0] * 100 + a[1] * 10 + a[2]; })'
    expect: '321'
  - call: '({ long a[] = {7}; reverse_longs(a, 1); a[0]; })'
    expect: '7'
  - call: '({ long a[] = {7, 8}; reverse_longs(a, 0); a[0] * 10 + a[1]; })'
    expect: '78'
---
.text
.globl _reverse_longs
.p2align 2
_reverse_longs:
    add x1, x0, x1, lsl #3
1:
    sub x2, x1, x0
    cmp x2, #8
    b.le 2f
    ldr x3, [x1, #-8]!
    ldr x4, [x0]
    str x4, [x1]
    str x3, [x0], #8
    b 1b
2:
    ret
