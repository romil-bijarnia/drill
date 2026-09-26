---
id: c-013
title: Binary search
lang: c
tags: [algorithms]
family: bsearch
modes: [trace, recall, blank]
spec: int bsearch_index(const int *xs, int n, int target) returns the index or -1.
tests:
  - call: 'bsearch_index((int[]){1, 3, 5, 7, 9}, 5, 7)'
    expect: '3'
  - call: 'bsearch_index((int[]){1, 3, 5}, 3, 4)'
    expect: '-1'
  - call: 'bsearch_index(NULL, 0, 1)'
    expect: '-1'
---
int bsearch_index(const int *xs, int n, int target)
{
    int lo = 0, hi = n - 1;
    while (lo <= hi) {
        int mid = lo + (hi - lo) / 2;
        if (xs[mid] == target) return mid;
        if (xs[mid] < target) lo = mid + 1;
        else hi = mid - 1;
    }
    return -1;
}
