---
id: asm-014
title: Add as 32-bit ints
lang: asm
tags: [arithmetic, registers]
modes: [trace, recall, blank]
spec: long add_int(long a, long b) adds the low 32 bits of a and b the way C int arithmetic does and returns the sign-extended result: add in w registers (writing w0 zeroes the top half), then sxtw.
decl: 'long add_int(long, long);'
tests:
  - call: 'add_int(2, 3)'
    expect: '5'
  - call: 'add_int(2147483647, 1)'
    expect: '-2147483648'
  - call: 'add_int(-1, 1)'
    expect: '0'
  - call: 'add_int(4294967296, 5)'
    expect: '5'
---
.text
.globl _add_int
.p2align 2
_add_int:
    add w0, w0, w1
    sxtw x0, w0
    ret
