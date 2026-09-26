---
id: asm-025
title: Parity
lang: asm
tags: [bits, registers]
modes: [trace, recall, blank]
spec: long parity(long n) returns 1 when n has an odd number of set bits and 0 otherwise: eor each half into the other with a shifted operand, by 32, 16, 8, 4, 2 and 1, then keep bit 0.
decl: 'long parity(long);'
tests:
  - call: 'parity(7)'
    expect: '1'
  - call: 'parity(3)'
    expect: '0'
  - call: 'parity(0x1234)'
    expect: '1'
  - call: 'parity(-1)'
    expect: '0'
  - call: 'parity(0)'
    expect: '0'
---
.text
.globl _parity
.p2align 2
_parity:
    eor x0, x0, x0, lsr #32
    eor x0, x0, x0, lsr #16
    eor x0, x0, x0, lsr #8
    eor x0, x0, x0, lsr #4
    eor x0, x0, x0, lsr #2
    eor x0, x0, x0, lsr #1
    and x0, x0, #1
    ret
