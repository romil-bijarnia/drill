---
id: py-026
title: Memoised Fibonacci
lang: py
tags: [recursion, decorators]
modes: [trace, recall, blank]
spec: The nth Fibonacci number (F0 = 0, F1 = 1) with functools.lru_cache.
tests:
  - call: 'fib(50)'
    expect: '12586269025'
  - call: 'fib(0)'
    expect: '0'
  - call: 'fib(10)'
    expect: '55'
---
from functools import lru_cache


@lru_cache(maxsize=None)
def fib(n):
    return n if n < 2 else fib(n - 1) + fib(n - 2)
