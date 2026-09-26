---
id: asm-044
title: Length of a linked list
lang: asm
tags: [structs, memory, loops]
modes: [trace, recall, blank]
spec: struct node { long value; struct node *next; }; long list_len(const struct node *head) counts the nodes by pointer chasing: ldr x0, [x0, #8] loads next (offset 8, after the value) until it is NULL.
decl: 'struct node { long value; struct node *next; }; long list_len(const struct node *);'
tests:
  - call: 'list_len(&(struct node){1, &(struct node){2, &(struct node){3, 0}}})'
    expect: '3'
  - call: 'list_len(&(struct node){1, 0})'
    expect: '1'
  - call: 'list_len(NULL)'
    expect: '0'
---
.text
.globl _list_len
.p2align 2
_list_len:
    mov x1, #0
1:
    cbz x0, 2f
    add x1, x1, #1
    ldr x0, [x0, #8]
    b 1b
2:
    mov x0, x1
    ret
