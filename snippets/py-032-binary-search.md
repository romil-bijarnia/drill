---
id: py-032
title: Binary search
lang: py
tags: [algorithms]
family: bsearch
modes: [trace, recall, blank]
spec: Index of target in a sorted list, or -1, using binary search.
tests:
  - call: 'binary_search([1, 3, 5, 7, 9], 7)'
    expect: '3'
  - call: 'binary_search([1, 3, 5], 4)'
    expect: '-1'
  - call: 'binary_search([], 1)'
    expect: '-1'
---
def binary_search(xs, target):
    lo, hi = 0, len(xs) - 1
    while lo <= hi:
        mid = (lo + hi) // 2
        if xs[mid] == target:
            return mid
        if xs[mid] < target:
            lo = mid + 1
        else:
            hi = mid - 1
    return -1
