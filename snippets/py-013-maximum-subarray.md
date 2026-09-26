---
id: py-013
title: Maximum subarray
lang: py
tags: [algorithms]
modes: [trace, recall, blank]
spec: Largest sum of any contiguous non-empty subarray (Kadane).
tests:
  - call: 'max_subarray([-2, 1, -3, 4, -1, 2, 1, -5, 4])'
    expect: '6'
  - call: 'max_subarray([-1])'
    expect: '-1'
  - call: 'max_subarray([1, 2, 3])'
    expect: '6'
---
def max_subarray(xs):
    best = current = xs[0]
    for x in xs[1:]:
        current = max(x, current + x)
        best = max(best, current)
    return best
