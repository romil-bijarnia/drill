---
id: asm-053
title: Copy bytes
lang: asm
tags: [loops, memory]
family: memcpy
modes: [trace, recall, blank]
spec: char *copy_bytes(char *dst, const char *src, long n) copies n bytes from src to dst with ldrb/strb through a register offset and returns dst. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'char *copy_bytes(char *, const char *, long);'
tests:
  - call: 'copy_bytes((char[8]){0}, "hello", 6)'
    expect: 'hello'
  - call: 'copy_bytes((char[8]){0}, "hello", 3)'
    expect: 'hel'
  - call: 'copy_bytes((char[4]){"xyz"}, "abc", 0)'
    expect: 'xyz'
  - call: 'copy_bytes((char[4]){0}, "abc", 4)'
    expect: 'abc'
---
.text
.globl _copy_bytes
.p2align 2
_copy_bytes:
    mov x3, #0
1:
    cmp x3, x2
    b.ge 2f
    ldrb w4, [x1, x3]
    strb w4, [x0, x3]
    add x3, x3, #1
    b 1b
2:
    ret
