---
id: asm-035
title: Compare strings
lang: asm
tags: [strings, loops]
modes: [trace, recall, blank]
spec: long str_cmp(const char *a, const char *b) returns the difference of the first bytes that differ, or 0 when the strings are equal: post-indexed ldrb walks both, b.ne leaves on a mismatch and cbnz keeps going until the shared terminator.
decl: 'long str_cmp(const char *, const char *);'
tests:
  - call: 'str_cmp("abc", "abc")'
    expect: '0'
  - call: 'str_cmp("abc", "abd")'
    expect: '-1'
  - call: 'str_cmp("b", "a")'
    expect: '1'
  - call: 'str_cmp("ab", "abc")'
    expect: '-99'
  - call: 'str_cmp("", "a")'
    expect: '-97'
---
.text
.globl _str_cmp
.p2align 2
_str_cmp:
1:
    ldrb w2, [x0], #1
    ldrb w3, [x1], #1
    cmp w2, w3
    b.ne 2f
    cbnz w2, 1b
    mov x0, #0
    ret
2:
    sub x0, x2, x3
    ret
