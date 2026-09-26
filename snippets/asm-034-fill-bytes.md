---
id: asm-034
title: Fill bytes
lang: asm
tags: [loops, memory, strings]
modes: [trace, recall, blank]
spec: char *fill_bytes(char *p, long n, long c) stores the byte c into the first n bytes at p with a post-indexed strb and returns p, like memset; the cursor is a copy so x0 survives.
decl: 'char *fill_bytes(char *, long, long);'
tests:
  - call: 'fill_bytes((char[]){"hello"}, 4, '-')'
    expect: '----o'
  - call: 'fill_bytes((char[]){"abc"}, 3, 'z')'
    expect: 'zzz'
  - call: 'fill_bytes((char[]){"hi"}, 0, 'x')'
    expect: 'hi'
---
.text
.globl _fill_bytes
.p2align 2
_fill_bytes:
    mov x3, x0
1:
    cbz x1, 2f
    strb w2, [x3], #1
    sub x1, x1, #1
    b 1b
2:
    ret
