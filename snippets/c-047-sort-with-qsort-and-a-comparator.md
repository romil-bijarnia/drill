---
id: c-047
title: Sort with qsort and a comparator
lang: c
tags: [functions, sorting, pointers]
modes: [trace, recall, blank]
spec: int cmp_int(const void *a, const void *b) returns -1, 0 or 1 for the two ints behind the pointers; int *sort_ints(int *xs, size_t n) sorts ascending in place with qsort and cmp_int and returns xs.
tests:
  - call: 'sort_ints((int[]){3, 1, 2}, 3)[0]'
    expect: '1'
  - call: '({ int a[] = {3, 1, 2}; sort_ints(a, 3); a[0] * 100 + a[1] * 10 + a[2]; })'
    expect: '123'
  - call: 'sort_ints((int[]){5, -1, 4, -1, 0}, 5)[4]'
    expect: '5'
  - call: 'cmp_int(&(int){5}, &(int){2})'
    expect: '1'
  - call: 'sort_ints((int[]){7}, 1)[0]'
    expect: '7'
---
int cmp_int(const void *a, const void *b)
{
    int x = *(const int *)a, y = *(const int *)b;
    return (x > y) - (x < y);
}

int *sort_ints(int *xs, size_t n)
{
    qsort(xs, n, sizeof *xs, cmp_int);
    return xs;
}
