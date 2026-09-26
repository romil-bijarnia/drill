---
id: asm-004
title: Sum an array
lang: asm
tags: [loops, memory]
family: sum
modes: [trace, recall, blank]
spec: long sum_array(const long *xs, long n): loop with a post-indexed ldr. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long sum_array(const long *, long);'
tests:
  - call: 'sum_array((long[]){1, 2, 3, 4}, 4)'
    expect: '10'
  - call: 'sum_array(NULL, 0)'
    expect: '0'
---
.text
.globl _sum_array
.p2align 2
_sum_array:
    mov x2, #0
1:
    cbz x1, 2f
    ldr x3, [x0], #8
    add x2, x2, x3
    sub x1, x1, #1
    b 1b
2:
    mov x0, x2
    ret
