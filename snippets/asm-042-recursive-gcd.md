---
id: asm-042
title: Recursive gcd
lang: asm
tags: [recursion, calls, arithmetic]
modes: [trace, recall, blank]
spec: long gcd_rec(long a, long b) is Euclid's gcd(b, a mod b) as a tail call: b == 0 returns a; otherwise udiv and msub give a mod b, the pair (b, a mod b) moves into x0 and x1, and a plain b instead of bl re-enters the function, so no frame is needed.
decl: 'long gcd_rec(long, long);'
tests:
  - call: 'gcd_rec(48, 18)'
    expect: '6'
  - call: 'gcd_rec(7, 13)'
    expect: '1'
  - call: 'gcd_rec(12, 12)'
    expect: '12'
  - call: 'gcd_rec(0, 5)'
    expect: '5'
  - call: 'gcd_rec(5, 0)'
    expect: '5'
---
.text
.globl _gcd_rec
.p2align 2
_gcd_rec:
    cbz x1, 1f
    udiv x2, x0, x1
    msub x2, x2, x1, x0
    mov x0, x1
    mov x1, x2
    b _gcd_rec
1:
    ret
