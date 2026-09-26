---
id: asm-027
title: Popcount with cnt
lang: asm
tags: [bits, registers]
modes: [trace, recall, blank]
spec: long popcnt(long n) counts the set bits of n without a loop: fmov moves n into d0, cnt counts the bits of each byte of v0.8b, addv adds the eight counts into b0, and fmov brings the total back to w0.
decl: 'long popcnt(long);'
tests:
  - call: 'popcnt(7)'
    expect: '3'
  - call: 'popcnt(0)'
    expect: '0'
  - call: 'popcnt(0x0f0f)'
    expect: '8'
  - call: 'popcnt(-1)'
    expect: '64'
---
.text
.globl _popcnt
.p2align 2
_popcnt:
    fmov d0, x0
    cnt v0.8b, v0.8b
    addv b0, v0.8b
    fmov w0, s0
    ret
