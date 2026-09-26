---
id: asm-011
title: Dot product
lang: asm
tags: [loops, memory]
modes: [trace, recall, blank]
spec: long dot(const long *a, const long *b, long n) with madd. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long dot(const long *, const long *, long);'
tests:
  - call: 'dot((long[]){1, 2, 3}, (long[]){4, 5, 6}, 3)'
    expect: '32'
  - call: 'dot(NULL, NULL, 0)'
    expect: '0'
---
.text
.globl _dot
.p2align 2
_dot:
    mov x3, #0
1:
    cbz x2, 2f
    ldr x4, [x0], #8
    ldr x5, [x1], #8
    madd x3, x4, x5, x3
    sub x2, x2, #1
    b 1b
2:
    mov x0, x3
    ret
