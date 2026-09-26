---
id: asm-051
title: Insertion sort
lang: asm
tags: [loops, memory, algorithms]
family: isort
modes: [trace, recall, blank]
spec: long *isort(long *xs, long n) sorts n values in place with insertion sort and returns xs; walk a pointer back with ldur [x3, #-8] and a post-indexed str while the element before is larger. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long *isort(long *, long);'
tests:
  - call: 'isort((long[]){3, 1, 2}, 3)[0]'
    expect: '1'
  - call: 'isort((long[]){3, 1, 2}, 3)[1]'
    expect: '2'
  - call: 'isort((long[]){3, 1, 2}, 3)[2]'
    expect: '3'
  - call: 'isort((long[]){9, 7, 8, 1}, 4)[3]'
    expect: '9'
  - call: 'isort((long[]){7}, 1)[0]'
    expect: '7'
---
.text
.globl _isort
.p2align 2
_isort:
    mov x2, #1
1:
    cmp x2, x1
    b.ge 4f
    add x3, x0, x2, lsl #3
    ldr x4, [x3]
2:
    cmp x3, x0
    b.eq 3f
    ldur x5, [x3, #-8]
    cmp x5, x4
    b.le 3f
    str x5, [x3], #-8
    b 2b
3:
    str x4, [x3]
    add x2, x2, #1
    b 1b
4:
    ret
