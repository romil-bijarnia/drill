---
id: c-051
title: Next power of two
lang: c
tags: [bits]
modes: [trace, recall, blank]
spec: unsigned next_pow2(unsigned n) returns the smallest power of two >= n (1 for n = 0) by smearing the top set bit of n - 1 rightwards, then adding 1; n is below 2^31.
tests:
  - call: 'next_pow2(5)'
    expect: '8'
  - call: 'next_pow2(64)'
    expect: '64'
  - call: 'next_pow2(1000)'
    expect: '1024'
  - call: 'next_pow2(0)'
    expect: '1'
  - call: 'next_pow2(1)'
    expect: '1'
---
unsigned next_pow2(unsigned n)
{
    if (n == 0) return 1;
    n--;
    n |= n >> 1;
    n |= n >> 2;
    n |= n >> 4;
    n |= n >> 8;
    n |= n >> 16;
    return n + 1;
}
