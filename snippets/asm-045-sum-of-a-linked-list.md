---
id: asm-045
title: Sum of a linked list
lang: asm
tags: [structs, memory, addressing]
modes: [trace, recall, blank]
spec: struct node { long value; struct node *next; }; long list_sum(const struct node *head) adds up every value: ldp x1, x0, [x0] loads value and next in one go, so the same load also advances to the next node.
decl: 'struct node { long value; struct node *next; }; long list_sum(const struct node *);'
tests:
  - call: 'list_sum(&(struct node){1, &(struct node){2, &(struct node){3, 0}}})'
    expect: '6'
  - call: 'list_sum(&(struct node){-5, 0})'
    expect: '-5'
  - call: 'list_sum(NULL)'
    expect: '0'
---
.text
.globl _list_sum
.p2align 2
_list_sum:
    mov x2, #0
1:
    cbz x0, 2f
    ldp x1, x0, [x0]
    add x2, x2, x1
    b 1b
2:
    mov x0, x2
    ret
