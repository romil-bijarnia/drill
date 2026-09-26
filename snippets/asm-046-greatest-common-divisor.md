---
id: asm-046
title: Greatest common divisor
lang: asm
tags: [loops, arithmetic]
family: gcd
modes: [trace, recall, blank]
spec: long gcd(long a, long b) of two non-negative integers by Euclid's algorithm in a loop, using udiv and msub for the remainder. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long gcd(long, long);'
tests:
  - call: 'gcd(12, 18)'
    expect: '6'
  - call: 'gcd(7, 3)'
    expect: '1'
  - call: 'gcd(0, 5)'
    expect: '5'
  - call: 'gcd(20, 0)'
    expect: '20'
---
.text
.globl _gcd
.p2align 2
_gcd:
1:
    cbz x1, 2f
    udiv x2, x0, x1
    msub x2, x2, x1, x0
    mov x0, x1
    mov x1, x2
    b 1b
2:
    ret
