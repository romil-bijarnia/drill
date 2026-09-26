---
id: c-006
title: Prime check
lang: c
tags: [algorithms]
modes: [trace, recall, blank]
spec: bool is_prime(int n): true when n is prime, false below 2.
tests:
  - call: 'is_prime(2)'
    expect: 'true'
  - call: 'is_prime(15)'
    expect: 'false'
  - call: 'is_prime(97)'
    expect: 'true'
  - call: 'is_prime(1)'
    expect: 'false'
---
bool is_prime(int n)
{
    if (n < 2) return false;
    for (int i = 2; i * i <= n; i++)
        if (n % i == 0) return false;
    return true;
}
