---
id: asm-006
title: Factorial
lang: asm
tags: [loops]
modes: [trace, recall, blank]
spec: long fact(long n) iteratively with mul; fact(0) is 1. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long fact(long);'
tests:
  - call: 'fact(5)'
    expect: '120'
  - call: 'fact(0)'
    expect: '1'
  - call: 'fact(10)'
    expect: '3628800'
---
.text
.globl _fact
.p2align 2
_fact:
    mov x1, #1
1:
    cmp x0, #1
    b.le 2f
    mul x1, x1, x0
    sub x0, x0, #1
    b 1b
2:
    mov x0, x1
    ret
