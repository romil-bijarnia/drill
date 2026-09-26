---
id: asm-048
title: Parse an integer
lang: asm
tags: [loops, memory, arithmetic]
family: atoi
modes: [trace, recall, blank]
spec: long parse_int(const char *s) turns a string of decimal digits with an optional leading minus into a long; ldrb each byte, subtract '0', multiply-accumulate with madd, negate at the end if needed. AArch64 macOS ABI: arguments in x0, x1, x2..., result in x0; labels need a leading underscore.
decl: 'long parse_int(const char *);'
tests:
  - call: 'parse_int("42")'
    expect: '42'
  - call: 'parse_int("-17")'
    expect: '-17'
  - call: 'parse_int("0")'
    expect: '0'
  - call: 'parse_int("1234567890")'
    expect: '1234567890'
---
.text
.globl _parse_int
.p2align 2
_parse_int:
    mov x1, #0
    mov x2, #0
    mov x4, #10
    ldrb w3, [x0]
    cmp w3, #'-'
    b.ne 1f
    mov x2, #1
    add x0, x0, #1
1:
    ldrb w3, [x0], #1
    cbz w3, 2f
    sub w3, w3, #'0'
    madd x1, x1, x4, x3
    b 1b
2:
    cbz x2, 3f
    neg x1, x1
3:
    mov x0, x1
    ret
