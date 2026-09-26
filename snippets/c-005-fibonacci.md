---
id: c-005
title: Fibonacci
lang: c
tags: [loops]
family: fib
modes: [trace, recall, blank]
spec: long fib(int n) returns the nth Fibonacci number (F0 = 0, F1 = 1) iteratively.
tests:
  - call: 'fib(10)'
    expect: '55'
  - call: 'fib(0)'
    expect: '0'
  - call: 'fib(1)'
    expect: '1'
---
long fib(int n)
{
    long a = 0, b = 1;
    for (int i = 0; i < n; i++) {
        long next = a + b;
        a = b;
        b = next;
    }
    return a;
}
