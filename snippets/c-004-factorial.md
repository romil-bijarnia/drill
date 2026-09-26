---
id: c-004
title: Factorial
lang: c
tags: [loops]
modes: [trace, recall, blank]
spec: long fact(int n) returns n! iteratively; fact(0) is 1.
tests:
  - call: 'fact(5)'
    expect: '120'
  - call: 'fact(0)'
    expect: '1'
  - call: 'fact(10)'
    expect: '3628800'
---
long fact(int n)
{
    long result = 1;
    for (int i = 2; i <= n; i++) result *= i;
    return result;
}
