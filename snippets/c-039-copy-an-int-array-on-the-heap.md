---
id: c-039
title: Copy an int array on the heap
lang: c
tags: [memory, arrays]
modes: [trace, recall, blank]
spec: int *copy_ints(const int *xs, size_t n) returns a malloc'd copy of the n ints that the caller frees; NULL when malloc fails.
tests:
  - call: '({ int *c = copy_ints((int[]){1, 2, 3}, 3); int v = c[0] * 100 + c[1] * 10 + c[2]; free(c); v; })'
    expect: '123'
  - call: '({ int a[] = {4, 5}; int *c = copy_ints(a, 2); c[0] = 9; int v = a[0]; free(c); v; })'
    expect: '4'
  - call: '({ int *c = copy_ints((int[]){-7}, 1); int v = *c; free(c); v; })'
    expect: '-7'
  - call: '({ int a[] = {4, 5}; int *c = copy_ints(a, 2); int same = c == a; free(c); same; })'
    expect: '0'
---
int *copy_ints(const int *xs, size_t n)
{
    int *copy = malloc(n * sizeof *copy);
    if (copy) memcpy(copy, xs, n * sizeof *copy);
    return copy;
}
