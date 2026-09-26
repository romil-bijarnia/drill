---
id: asm-008
title: Population count
lang: asm
tags: [bits, loops]
modes: [trace, recall, blank]
spec: long popcount(long n): count set bits with and, lsr and a loop. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long popcount(long);'
tests:
  - call: 'popcount(7)'
    expect: '3'
  - call: 'popcount(0)'
    expect: '0'
  - call: 'popcount(255)'
    expect: '8'
---
.text
.globl _popcount
.p2align 2
_popcount:
    mov x1, #0
1:
    cbz x0, 2f
    and x2, x0, #1
    add x1, x1, x2
    lsr x0, x0, #1
    b 1b
2:
    mov x0, x1
    ret
