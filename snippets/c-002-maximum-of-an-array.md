---
id: c-002
title: Maximum of an array
lang: c
tags: [arrays]
family: max
modes: [trace, recall, blank]
spec: int max_of(const int *xs, int n) returns the largest of n values (n >= 1).
tests:
  - call: 'max_of((int[]){3, 9, 2}, 3)'
    expect: '9'
  - call: 'max_of((int[]){-5}, 1)'
    expect: '-5'
---
int max_of(const int *xs, int n)
{
    int best = xs[0];
    for (int i = 1; i < n; i++)
        if (xs[i] > best) best = xs[i];
    return best;
}
