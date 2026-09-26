---
id: c-026
title: Pointer to the max element
lang: c
tags: [pointers, arrays, search]
modes: [trace, recall, blank]
spec: const int *max_ptr(const int *xs, size_t n) returns a pointer to the largest of n >= 1 ints, the first one on ties.
tests:
  - call: '*max_ptr((int[]){3, 9, 2}, 3)'
    expect: '9'
  - call: '({ int a[] = {3, 9, 2}; max_ptr(a, 3) - a; })'
    expect: '1'
  - call: '({ int a[] = {4, 4}; max_ptr(a, 2) - a; })'
    expect: '0'
  - call: '*max_ptr((int[]){-5}, 1)'
    expect: '-5'
---
const int *max_ptr(const int *xs, size_t n)
{
    const int *best = xs;
    for (const int *p = xs + 1; p < xs + n; p++)
        if (*p > *best) best = p;
    return best;
}
