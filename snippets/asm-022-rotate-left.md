---
id: asm-022
title: Rotate left
lang: asm
tags: [bits, arithmetic]
modes: [trace, recall, blank]
spec: long rotl(long x, long n) rotates x left by n bits (0 <= n < 64): AArch64 only has ror, so rotate right by 64 - n, which neg produces because ror only reads the low six bits of the amount.
decl: 'long rotl(long, long);'
tests:
  - call: 'rotl(1, 1)'
    expect: '2'
  - call: 'rotl(0x0f, 4)'
    expect: '240'
  - call: 'rotl(1, 63)'
    expect: '-9223372036854775808'
  - call: 'rotl(5, 0)'
    expect: '5'
---
.text
.globl _rotl
.p2align 2
_rotl:
    neg x1, x1
    ror x0, x0, x1
    ret
