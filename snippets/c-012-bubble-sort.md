---
id: c-012
title: Bubble sort
lang: c
tags: [algorithms, arrays]
modes: [trace, recall, blank]
spec: void bubble_sort(int *xs, int n) sorts in place.
tests:
  - call: '({ int a[] = {3, 1, 2}; bubble_sort(a, 3); a[0] * 100 + a[1] * 10 + a[2]; })'
    expect: '123'
  - call: '({ int a[] = {9, 7, 8, 1}; bubble_sort(a, 4); a[0] * 1000 + a[1] * 100 + a[2] * 10 + a[3]; })'
    expect: '1789'
---
void bubble_sort(int *xs, int n)
{
    for (int pass = 0; pass < n - 1; pass++)
        for (int i = 0; i < n - 1 - pass; i++)
            if (xs[i] > xs[i + 1]) {
                int tmp = xs[i];
                xs[i] = xs[i + 1];
                xs[i + 1] = tmp;
            }
}
