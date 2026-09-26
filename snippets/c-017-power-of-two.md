---
id: c-017
title: Power of two
lang: c
tags: [bits]
modes: [trace, recall, blank]
spec: bool is_pow2(unsigned n): true for 1, 2, 4, 8...; false for 0.
tests:
  - call: 'is_pow2(8)'
    expect: 'true'
  - call: 'is_pow2(6)'
    expect: 'false'
  - call: 'is_pow2(0)'
    expect: 'false'
  - call: 'is_pow2(1)'
    expect: 'true'
---
bool is_pow2(unsigned n)
{
    return n != 0 && (n & (n - 1)) == 0;
}
