---
id: c-029
title: Rotate an array left
lang: c
tags: [arrays, pointers]
modes: [trace, recall, blank]
spec: int *rotate_left(int *xs, int n, int k) rotates n >= 1 ints left by k places in place (k may exceed n) and returns xs; reverse the first k, the rest, then the whole array.
tests:
  - call: '({ int a[] = {1, 2, 3, 4, 5}; rotate_left(a, 5, 2); a[0] * 100 + a[1] * 10 + a[4]; })'
    expect: '342'
  - call: 'rotate_left((int[]){1, 2, 3}, 3, 0)[0]'
    expect: '1'
  - call: 'rotate_left((int[]){1, 2, 3}, 3, 4)[0]'
    expect: '2'
  - call: 'rotate_left((int[]){7}, 1, 3)[0]'
    expect: '7'
---
static void reverse_range(int *lo, int *hi)
{
    for (hi--; lo < hi; lo++, hi--) {
        int tmp = *lo;
        *lo = *hi;
        *hi = tmp;
    }
}

int *rotate_left(int *xs, int n, int k)
{
    k %= n;
    reverse_range(xs, xs + k);
    reverse_range(xs + k, xs + n);
    reverse_range(xs, xs + n);
    return xs;
}
