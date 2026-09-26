---
id: c-001
title: Add two ints
lang: c
tags: [basics]
modes: [trace, recall, blank]
spec: int add(int a, int b) returns their sum.
tests:
  - call: 'add(2, 3)'
    expect: '5'
  - call: 'add(-1, 1)'
    expect: '0'
---
int add(int a, int b)
{
    return a + b;
}
