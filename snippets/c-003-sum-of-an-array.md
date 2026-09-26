---
id: c-003
title: Sum of an array
lang: c
tags: [arrays]
modes: [trace, recall, blank]
spec: long sum(const int *xs, int n) returns the total of n values.
tests:
  - call: 'sum((int[]){1, 2, 3, 4}, 4)'
    expect: '10'
  - call: 'sum(NULL, 0)'
    expect: '0'
---
long sum(const int *xs, int n)
{
    long total = 0;
    for (int i = 0; i < n; i++) total += xs[i];
    return total;
}
