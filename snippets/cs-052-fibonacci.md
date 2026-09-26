---
id: cs-052
title: Fibonacci
lang: cs
tags: [loops, arithmetic]
family: fib
modes: [trace, recall, blank]
spec: long Fib(long n) returns the nth Fibonacci number iteratively, with Fib(0) = 0 and Fib(1) = 1; no recursion.
tests:
  - call: 'Fib(10)'
    expect: '55'
  - call: 'Fib(0)'
    expect: '0'
  - call: 'Fib(1)'
    expect: '1'
  - call: 'Fib(50)'
    expect: '12586269025'
---
public static long Fib(long n)
{
    long a = 0, b = 1;
    for (long i = 0; i < n; i++)
        (a, b) = (b, a + b);
    return a;
}
