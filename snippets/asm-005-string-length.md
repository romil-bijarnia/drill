---
id: asm-005
title: String length
lang: asm
tags: [loops, memory]
modes: [trace, recall, blank]
spec: long str_len(const char *s): count bytes until the zero terminator with ldrb. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long str_len(const char *);'
tests:
  - call: 'str_len("hello")'
    expect: '5'
  - call: 'str_len("")'
    expect: '0'
---
.text
.globl _str_len
.p2align 2
_str_len:
    mov x1, #0
1:
    ldrb w2, [x0, x1]
    cbz w2, 2f
    add x1, x1, #1
    b 1b
2:
    mov x0, x1
    ret
