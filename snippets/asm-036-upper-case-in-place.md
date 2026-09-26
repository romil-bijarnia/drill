---
id: asm-036
title: Upper case in place
lang: asm
tags: [strings, loops, memory]
modes: [trace, recall, blank]
spec: char *to_upper(char *s) converts a-z to A-Z in place and returns s: subtracting 'a' and comparing unsigned with 25 (b.hi) rejects everything outside the range in one test, then strb writes the byte back.
decl: 'char *to_upper(char *);'
tests:
  - call: 'to_upper((char[]){"Hello, World"})'
    expect: 'HELLO, WORLD'
  - call: 'to_upper((char[]){"a1z"})'
    expect: 'A1Z'
  - call: 'to_upper((char[]){"a`{z"})'
    expect: 'A`{Z'
  - call: 'to_upper((char[]){"ABC"})'
    expect: 'ABC'
---
.text
.globl _to_upper
.p2align 2
_to_upper:
    mov x1, x0
1:
    ldrb w2, [x1]
    cbz w2, 3f
    sub w3, w2, #'a'
    cmp w3, #25
    b.hi 2f
    sub w2, w2, #32
    strb w2, [x1]
2:
    add x1, x1, #1
    b 1b
3:
    ret
