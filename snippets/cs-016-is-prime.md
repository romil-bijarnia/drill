---
id: cs-016
title: Prime check
tags: [algorithms]
modes: [trace, recall, blank]
spec: Return true when n is prime; numbers below 2 are not prime.
tests:
  - call: 'IsPrime(2)'
    expect: 'True'
  - call: 'IsPrime(15)'
    expect: 'False'
  - call: 'IsPrime(97)'
    expect: 'True'
  - call: 'IsPrime(1)'
    expect: 'False'
---
public static bool IsPrime(int n)
{
    if (n < 2) return false;
    for (int i = 2; i * i <= n; i++)
        if (n % i == 0) return false;
    return true;
}
