---
id: c-021
title: Clamp
lang: c
tags: [basics]
modes: [trace, recall, blank]
spec: int clamp(int v, int lo, int hi) limits v to the range.
tests:
  - call: 'clamp(15, 0, 10)'
    expect: '10'
  - call: 'clamp(-3, 0, 10)'
    expect: '0'
  - call: 'clamp(5, 0, 10)'
    expect: '5'
---
int clamp(int v, int lo, int hi)
{
    return v < lo ? lo : v > hi ? hi : v;
}
