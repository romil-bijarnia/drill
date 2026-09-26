---
id: asm-052
title: Palindrome
lang: asm
tags: [loops, memory, strings]
family: palindrome
modes: [trace, recall, blank]
spec: _Bool is_palindrome(const char *s) returns 1 in x0 when s reads the same backwards and 0 otherwise; find the last byte with ldrb, then compare bytes from both ends inwards. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: '_Bool is_palindrome(const char *);'
tests:
  - call: 'is_palindrome("racecar")'
    expect: 'true'
  - call: 'is_palindrome("hello")'
    expect: 'false'
  - call: 'is_palindrome("abba")'
    expect: 'true'
  - call: 'is_palindrome("")'
    expect: 'true'
---
.text
.globl _is_palindrome
.p2align 2
_is_palindrome:
    mov x1, x0
1:
    ldrb w2, [x1]
    cbz w2, 2f
    add x1, x1, #1
    b 1b
2:
    sub x1, x1, #1
3:
    cmp x0, x1
    b.hs 4f
    ldrb w2, [x0], #1
    ldrb w3, [x1], #-1
    cmp w2, w3
    b.eq 3b
    mov x0, #0
    ret
4:
    mov x0, #1
    ret
