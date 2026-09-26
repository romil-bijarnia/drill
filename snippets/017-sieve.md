---
id: 017
title: Sieve of Eratosthenes
tags: [algorithms]
modes: [trace, recall, blank]
spec: Return every prime up to and including n using a sieve.
tests:
  - call: 'string.Join(",", Primes(20))'
    expect: '2,3,5,7,11,13,17,19'
  - call: 'string.Join(",", Primes(1))'
    expect: ''
  - call: 'string.Join(",", Primes(2))'
    expect: '2'
---
public static List<int> Primes(int n)
{
    var composite = new bool[n + 1];
    var primes = new List<int>();
    for (int i = 2; i <= n; i++)
    {
        if (composite[i]) continue;
        primes.Add(i);
        for (long j = (long)i * i; j <= n; j += i) composite[j] = true;
    }
    return primes;
}
