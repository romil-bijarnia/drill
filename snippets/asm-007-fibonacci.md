---
id: asm-007
title: Fibonacci
lang: asm
tags: [loops]
modes: [trace, recall, blank]
spec: long fib(long n) iteratively (F0 = 0, F1 = 1). AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long fib(long);'
tests:
  - call: 'fib(10)'
    expect: '55'
  - call: 'fib(0)'
    expect: '0'
  - call: 'fib(1)'
    expect: '1'
---
.text
.globl _fib
.p2align 2
_fib:
    mov x1, #0
    mov x2, #1
1:
    cbz x0, 2f
    add x3, x1, x2
    mov x1, x2
    mov x2, x3
    sub x0, x0, #1
    b 1b
2:
    mov x0, x1
    ret
