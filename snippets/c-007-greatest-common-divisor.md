---
id: c-007
title: Greatest common divisor
lang: c
tags: [recursion]
family: gcd
modes: [trace, recall, blank]
spec: int gcd(int a, int b), recursively.
tests:
  - call: 'gcd(12, 18)'
    expect: '6'
  - call: 'gcd(7, 3)'
    expect: '1'
  - call: 'gcd(0, 5)'
    expect: '5'
---
int gcd(int a, int b)
{
    return b == 0 ? a : gcd(b, a % b);
}
