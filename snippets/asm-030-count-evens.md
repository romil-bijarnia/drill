---
id: asm-030
title: Count evens
lang: asm
tags: [loops, memory, bits]
modes: [trace, recall, blank]
spec: long count_even(const long *xs, long n) returns how many of the n values are even: a post-indexed ldr walks the array and tbnz on bit 0 jumps past the increment for odd values.
decl: 'long count_even(const long *, long);'
tests:
  - call: 'count_even((long[]){1, 2, 3, 4, 6}, 5)'
    expect: '3'
  - call: 'count_even((long[]){1, 3, 5}, 3)'
    expect: '0'
  - call: 'count_even((long[]){-2, 0}, 2)'
    expect: '2'
  - call: 'count_even(NULL, 0)'
    expect: '0'
---
.text
.globl _count_even
.p2align 2
_count_even:
    mov x2, #0
1:
    cbz x1, 2f
    ldr x3, [x0], #8
    sub x1, x1, #1
    tbnz x3, #0, 1b
    add x2, x2, #1
    b 1b
2:
    mov x0, x2
    ret
