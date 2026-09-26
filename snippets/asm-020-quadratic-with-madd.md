---
id: asm-020
title: Quadratic with madd
lang: asm
tags: [arithmetic, registers]
modes: [trace, recall, blank]
spec: long quad(long x, long a, long b, long c) returns a*x*x + b*x + c by Horner's rule, (a*x + b)*x + c, in two madd instructions.
decl: 'long quad(long, long, long, long);'
tests:
  - call: 'quad(2, 1, 0, 0)'
    expect: '4'
  - call: 'quad(3, 2, -1, 5)'
    expect: '20'
  - call: 'quad(0, 5, 5, 7)'
    expect: '7'
  - call: 'quad(-1, 1, 1, 1)'
    expect: '1'
---
.text
.globl _quad
.p2align 2
_quad:
    madd x1, x1, x0, x2
    madd x0, x1, x0, x3
    ret
