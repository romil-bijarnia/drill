---
id: py-007
title: Sieve of Eratosthenes
lang: py
tags: [algorithms]
modes: [trace, recall, blank]
spec: Return the list of primes up to and including n using a sieve.
tests:
  - call: 'primes(20)'
    expect: '[2, 3, 5, 7, 11, 13, 17, 19]'
  - call: 'primes(1)'
    expect: '[]'
  - call: 'primes(2)'
    expect: '[2]'
---
def primes(n):
    composite = [False] * (n + 1)
    result = []
    for i in range(2, n + 1):
        if composite[i]:
            continue
        result.append(i)
        for j in range(i * i, n + 1, i):
            composite[j] = True
    return result
