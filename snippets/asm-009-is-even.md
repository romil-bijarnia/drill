---
id: asm-009
title: Is even
lang: asm
tags: [bits]
modes: [trace, recall, blank]
spec: long is_even(long n): 1 when even, 0 when odd, without branching. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long is_even(long);'
tests:
  - call: 'is_even(4)'
    expect: '1'
  - call: 'is_even(7)'
    expect: '0'
---
.text
.globl _is_even
.p2align 2
_is_even:
    and x0, x0, #1
    eor x0, x0, #1
    ret
