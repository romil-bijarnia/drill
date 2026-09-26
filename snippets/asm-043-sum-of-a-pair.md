---
id: asm-043
title: Sum of a pair
lang: asm
tags: [structs, memory]
modes: [trace, recall, blank]
spec: struct pair { long a; long b; }; long sum_pair(const struct pair *p) returns p->a + p->b: the two fields sit next to each other, so a single ldp loads both.
decl: 'struct pair { long a; long b; }; long sum_pair(const struct pair *);'
tests:
  - call: 'sum_pair(&(struct pair){3, 4})'
    expect: '7'
  - call: 'sum_pair(&(struct pair){-10, 4})'
    expect: '-6'
  - call: 'sum_pair(&(struct pair){0, 0})'
    expect: '0'
---
.text
.globl _sum_pair
.p2align 2
_sum_pair:
    ldp x1, x2, [x0]
    add x0, x1, x2
    ret
