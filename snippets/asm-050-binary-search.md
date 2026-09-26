---
id: asm-050
title: Binary search
lang: asm
tags: [loops, memory, algorithms]
family: bsearch
modes: [trace, recall, blank]
spec: long bsearch_idx(const long *xs, long n, long target) returns the index of target in a sorted array or -1; lo/hi halving loop with a scaled ldr [x0, x5, lsl #3]. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long bsearch_idx(const long *, long, long);'
tests:
  - call: 'bsearch_idx((long[]){1, 3, 5, 7, 9}, 5, 7)'
    expect: '3'
  - call: 'bsearch_idx((long[]){1, 3, 5}, 3, 4)'
    expect: '-1'
  - call: 'bsearch_idx(NULL, 0, 1)'
    expect: '-1'
  - call: 'bsearch_idx((long[]){1, 3, 5, 7, 9}, 5, 1)'
    expect: '0'
---
.text
.globl _bsearch_idx
.p2align 2
_bsearch_idx:
    mov x3, #0
    sub x4, x1, #1
1:
    cmp x3, x4
    b.gt 3f
    add x5, x3, x4
    lsr x5, x5, #1
    ldr x6, [x0, x5, lsl #3]
    cmp x6, x2
    b.eq 2f
    b.gt 4f
    add x3, x5, #1
    b 1b
4:
    sub x4, x5, #1
    b 1b
3:
    mov x5, #-1
2:
    mov x0, x5
    ret
