---
id: py-006
title: Prime check
lang: py
tags: [algorithms]
modes: [trace, recall, blank]
spec: Return True when n is prime; numbers below 2 are not.
tests:
  - call: 'is_prime(2)'
    expect: 'True'
  - call: 'is_prime(15)'
    expect: 'False'
  - call: 'is_prime(97)'
    expect: 'True'
  - call: 'is_prime(1)'
    expect: 'False'
---
def is_prime(n):
    if n < 2:
        return False
    i = 2
    while i * i <= n:
        if n % i == 0:
            return False
        i += 1
    return True
