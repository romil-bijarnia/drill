---
id: py-040
title: Index of the maximum
lang: py
tags: [basics]
modes: [trace, recall, blank]
spec: Index of the largest value in a non-empty list using enumerate; the first if tied.
tests:
  - call: 'index_of_max([3, 9, 2])'
    expect: '1'
  - call: 'index_of_max([5, 5])'
    expect: '0'
---
def index_of_max(xs):
    best = 0
    for i, x in enumerate(xs):
        if x > xs[best]:
            best = i
    return best
