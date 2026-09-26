---
id: asm-021
title: Extract a byte
lang: asm
tags: [bits, registers]
modes: [trace, recall, blank]
spec: long byte_at(long x, long i) returns byte i of x, byte 0 being the lowest: lsl turns the byte index into a bit count, lsr by that register amount, then and with 0xff.
decl: 'long byte_at(long, long);'
tests:
  - call: 'byte_at(0x1234, 0)'
    expect: '52'
  - call: 'byte_at(0x1234, 1)'
    expect: '18'
  - call: 'byte_at(-1, 7)'
    expect: '255'
  - call: 'byte_at(0x1234, 5)'
    expect: '0'
---
.text
.globl _byte_at
.p2align 2
_byte_at:
    lsl x1, x1, #3
    lsr x0, x0, x1
    and x0, x0, #0xff
    ret
