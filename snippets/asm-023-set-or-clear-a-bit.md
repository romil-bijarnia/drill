---
id: asm-023
title: Set or clear a bit
lang: asm
tags: [bits, branches]
modes: [trace, recall, blank]
spec: long put_bit(long x, long i, long on) returns x with bit i set when on is non-zero and cleared when it is zero: build the mask with lsl, clear the bit with bic, then orr in the mask or xzr as csel decides.
decl: 'long put_bit(long, long, long);'
tests:
  - call: 'put_bit(0, 3, 1)'
    expect: '8'
  - call: 'put_bit(15, 0, 0)'
    expect: '14'
  - call: 'put_bit(8, 3, 1)'
    expect: '8'
  - call: 'put_bit(-1, 63, 0)'
    expect: '9223372036854775807'
---
.text
.globl _put_bit
.p2align 2
_put_bit:
    mov x3, #1
    lsl x3, x3, x1
    bic x0, x0, x3
    cmp x2, #0
    csel x3, x3, xzr, ne
    orr x0, x0, x3
    ret
