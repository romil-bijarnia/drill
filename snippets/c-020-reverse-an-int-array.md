---
id: c-020
title: Reverse an int array
lang: c
tags: [arrays]
modes: [trace, recall, blank]
spec: void reverse_ints(int *xs, int n) reverses in place.
tests:
  - call: '({ int a[] = {1, 2, 3, 4}; reverse_ints(a, 4); a[0] * 1000 + a[1] * 100 + a[2] * 10 + a[3]; })'
    expect: '4321'
---
void reverse_ints(int *xs, int n)
{
    for (int i = 0, j = n - 1; i < j; i++, j--) {
        int tmp = xs[i];
        xs[i] = xs[j];
        xs[j] = tmp;
    }
}
