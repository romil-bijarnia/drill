---
id: asm-047
title: Reverse a string in place
lang: asm
tags: [loops, memory, strings]
family: reverse
modes: [trace, recall, blank]
spec: char *str_rev(char *s) reverses the zero-terminated string in place and returns s; find the end with ldrb, then swap bytes from both ends inwards. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'char *str_rev(char *);'
tests:
  - call: 'str_rev((char[]){"hello"})'
    expect: 'olleh'
  - call: 'str_rev((char[]){""})'
    expect: ''
  - call: 'str_rev((char[]){"ab"})'
    expect: 'ba'
  - call: 'str_rev((char[]){"abc"})'
    expect: 'cba'
---
.text
.globl _str_rev
.p2align 2
_str_rev:
    mov x1, x0
1:
    ldrb w2, [x1]
    cbz w2, 2f
    add x1, x1, #1
    b 1b
2:
    mov x3, x0
    sub x1, x1, #1
3:
    cmp x3, x1
    b.hs 4f
    ldrb w2, [x3]
    ldrb w4, [x1]
    strb w4, [x3], #1
    strb w2, [x1], #-1
    b 3b
4:
    ret
