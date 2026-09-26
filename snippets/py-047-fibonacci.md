---
id: py-047
title: Fibonacci
lang: py
tags: [loops, arithmetic]
family: fib
modes: [trace, recall, blank]
spec: fib(n) returns the nth Fibonacci number iteratively, with fib(0) = 0 and fib(1) = 1; no recursion.
tests:
  - call: 'fib(10)'
    expect: '55'
  - call: 'fib(0)'
    expect: '0'
  - call: 'fib(1)'
    expect: '1'
  - call: 'fib(50)'
    expect: '12586269025'
---
def fib(n):
    a, b = 0, 1
    for _ in range(n):
        a, b = b, a + b
    return a
