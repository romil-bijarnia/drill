---
id: asm-013
title: Multiply and negate
lang: asm
tags: [arithmetic, registers]
modes: [trace, recall, blank]
spec: long neg_product(long a, long b) returns -(a * b) with mul and neg.
decl: 'long neg_product(long, long);'
tests:
  - call: 'neg_product(3, 4)'
    expect: '-12'
  - call: 'neg_product(-2, 5)'
    expect: '10'
  - call: 'neg_product(7, 0)'
    expect: '0'
---
.text
.globl _neg_product
.p2align 2
_neg_product:
    mul x0, x0, x1
    neg x0, x0
    ret
