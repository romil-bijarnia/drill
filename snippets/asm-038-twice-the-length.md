---
id: asm-038
title: Twice the length
lang: asm
tags: [calls, stack]
modes: [trace, recall, blank]
spec: long twice_len(const char *s) returns 2 * strlen(s) by calling the C library's strlen: bl overwrites x30, so stp x29, x30, [sp, #-16]! saves the frame record first, mov x29, sp starts the frame, and ldp with post-index restores both before ret.
decl: 'long twice_len(const char *);'
tests:
  - call: 'twice_len("hello")'
    expect: '10'
  - call: 'twice_len("ab")'
    expect: '4'
  - call: 'twice_len("")'
    expect: '0'
---
.text
.globl _twice_len
.p2align 2
_twice_len:
    stp x29, x30, [sp, #-16]!
    mov x29, sp
    bl _strlen
    lsl x0, x0, #1
    ldp x29, x30, [sp], #16
    ret
