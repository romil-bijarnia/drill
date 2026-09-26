---
id: c-027
title: Count matches in an array
lang: c
tags: [arrays, basics]
modes: [trace, recall, blank]
spec: int count_eq(const int *xs, int n, int v) counts the elements equal to v; 0 when n is 0.
tests:
  - call: 'count_eq((int[]){1, 2, 1, 3, 1}, 5, 1)'
    expect: '3'
  - call: 'count_eq((int[]){1, 2}, 2, 9)'
    expect: '0'
  - call: 'count_eq(NULL, 0, 1)'
    expect: '0'
  - call: 'count_eq((int[]){-4}, 1, -4)'
    expect: '1'
---
int count_eq(const int *xs, int n, int v)
{
    int count = 0;
    for (int i = 0; i < n; i++)
        if (xs[i] == v) count++;
    return count;
}
