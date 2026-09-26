---
id: c-011
title: Swap through pointers
lang: c
tags: [pointers]
modes: [trace, recall, blank]
spec: void swap(int *a, int *b) exchanges the two ints.
tests:
  - call: '({ int a = 1, b = 2; swap(&a, &b); a * 10 + b; })'
    expect: '21'
---
void swap(int *a, int *b)
{
    int tmp = *a;
    *a = *b;
    *b = tmp;
}
