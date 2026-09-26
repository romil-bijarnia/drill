---
id: cs-009
title: Fibonacci iterator
tags: [iterators, yield]
modes: [trace, recall, blank]
spec: Yield the first n Fibonacci numbers starting from 0.
tests:
  - call: 'string.Join(",", Fib(5))'
    expect: '0,1,1,2,3'
  - call: 'string.Join(",", Fib(1))'
    expect: '0'
  - call: 'string.Join(",", Fib(0))'
    expect: ''
---
public static IEnumerable<long> Fib(int n)
{
    long a = 0, b = 1;
    for (int i = 0; i < n; i++)
    {
        yield return a;
        (a, b) = (b, a + b);
    }
}
