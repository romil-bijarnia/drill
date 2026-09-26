---
id: asm-003
title: Absolute value
lang: asm
tags: [compare]
modes: [trace, recall, blank]
spec: long abs64(long n) with cmp and cneg. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long abs64(long);'
tests:
  - call: 'abs64(-5)'
    expect: '5'
  - call: 'abs64(4)'
    expect: '4'
  - call: 'abs64(0)'
    expect: '0'
---
.text
.globl _abs64
.p2align 2
_abs64:
    cmp x0, #0
    cneg x0, x0, lt
    ret
