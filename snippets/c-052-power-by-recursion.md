---
id: c-052
title: Power by recursion
lang: c
tags: [recursion, basics]
modes: [trace, recall, blank]
spec: long power(long base, int exp) returns base raised to exp (exp >= 0) recursively by squaring the half power, so exp 1000 takes about 10 calls.
tests:
  - call: 'power(2, 10)'
    expect: '1024'
  - call: 'power(3, 4)'
    expect: '81'
  - call: 'power(-3, 3)'
    expect: '-27'
  - call: 'power(5, 0)'
    expect: '1'
  - call: 'power(0, 5)'
    expect: '0'
---
long power(long base, int exp)
{
    if (exp == 0) return 1;
    long half = power(base, exp / 2);
    return exp % 2 ? half * half * base : half * half;
}
