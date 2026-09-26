---
id: asm-012
title: Count a byte
lang: asm
tags: [loops, memory]
family: count_char
modes: [trace, recall, blank]
spec: long count_byte(const char *s, long c): occurrences of the byte c in a zero-terminated string. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long count_byte(const char *, long);'
tests:
  - call: 'count_byte("banana", 'a')'
    expect: '3'
  - call: 'count_byte("", 'x')'
    expect: '0'
---
.text
.globl _count_byte
.p2align 2
_count_byte:
    mov x2, #0
1:
    ldrb w3, [x0], #1
    cbz w3, 3f
    cmp w3, w1
    b.ne 1b
    add x2, x2, #1
    b 1b
3:
    mov x0, x2
    ret
