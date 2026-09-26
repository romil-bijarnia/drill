---
id: py-046
title: Population count
lang: py
tags: [bits, loops]
family: popcount
modes: [trace, recall, blank]
spec: popcount(x) counts the set bits of a non-negative int with a loop over & 1 and >>= 1; no bin() and no bit_count().
tests:
  - call: 'popcount(7)'
    expect: '3'
  - call: 'popcount(0)'
    expect: '0'
  - call: 'popcount(255)'
    expect: '8'
  - call: 'popcount(1024)'
    expect: '1'
---
def popcount(x):
    count = 0
    while x:
        count += x & 1
        x >>= 1
    return count
