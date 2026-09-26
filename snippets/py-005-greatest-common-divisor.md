---
id: py-005
title: Greatest common divisor
lang: py
tags: [algorithms, recursion]
modes: [trace, recall, blank]
spec: Greatest common divisor of two non-negative integers, recursively.
tests:
  - call: 'gcd(12, 18)'
    expect: '6'
  - call: 'gcd(7, 3)'
    expect: '1'
  - call: 'gcd(0, 5)'
    expect: '5'
---
def gcd(a, b):
    return a if b == 0 else gcd(b, a % b)
