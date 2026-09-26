---
id: c-023
title: Population count
lang: c
tags: [bits]
family: popcount
modes: [trace, recall, blank]
spec: int popcount(unsigned n) counts set bits with a loop.
tests:
  - call: 'popcount(7)'
    expect: '3'
  - call: 'popcount(0)'
    expect: '0'
  - call: 'popcount(255)'
    expect: '8'
---
int popcount(unsigned n)
{
    int count = 0;
    while (n) {
        count += n & 1u;
        n >>= 1;
    }
    return count;
}
