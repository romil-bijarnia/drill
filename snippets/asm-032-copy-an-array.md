---
id: asm-032
title: Copy an array
lang: asm
tags: [loops, memory, addressing]
modes: [trace, recall, blank]
spec: void copy_longs(long *dst, const long *src, long n) copies n values from src to dst with a post-indexed ldr and str pair, each advancing its pointer by 8 after the access.
decl: 'void copy_longs(long *, const long *, long);'
tests:
  - call: '({ long d[3] = {0}; copy_longs(d, (long[]){7, 8, 9}, 3); d[0] * 100 + d[1] * 10 + d[2]; })'
    expect: '789'
  - call: '({ long d[2] = {5, 5}; copy_longs(d, (long[]){1}, 1); d[0] * 10 + d[1]; })'
    expect: '15'
  - call: '({ long d[1] = {4}; copy_longs(d, NULL, 0); d[0]; })'
    expect: '4'
---
.text
.globl _copy_longs
.p2align 2
_copy_longs:
1:
    cbz x2, 2f
    ldr x3, [x1], #8
    str x3, [x0], #8
    sub x2, x2, #1
    b 1b
2:
    ret
