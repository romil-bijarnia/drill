---
id: asm-001
title: Add two longs
lang: asm
tags: [arithmetic]
modes: [trace, recall, blank]
spec: long add(long a, long b). AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long add(long, long);'
tests:
  - call: 'add(2, 3)'
    expect: '5'
  - call: 'add(-1, 1)'
    expect: '0'
---
.text
.globl _add
.p2align 2
_add:
    add x0, x0, x1
    ret
