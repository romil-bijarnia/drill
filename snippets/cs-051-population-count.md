---
id: cs-051
title: Population count
lang: cs
tags: [bits, loops]
family: popcount
modes: [trace, recall, blank]
spec: int PopCount(long x) counts the set bits of a non-negative long with a loop over & 1 and >>= 1; no BitOperations.
tests:
  - call: 'PopCount(7)'
    expect: '3'
  - call: 'PopCount(0)'
    expect: '0'
  - call: 'PopCount(255)'
    expect: '8'
  - call: 'PopCount(1024)'
    expect: '1'
---
public static int PopCount(long x)
{
    int count = 0;
    while (x != 0)
    {
        count += (int)(x & 1);
        x >>= 1;
    }
    return count;
}
