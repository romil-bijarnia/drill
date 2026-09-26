---
id: py-045
title: Maximum of a list
lang: py
tags: [loops, lists]
family: max
modes: [trace, recall, blank]
spec: max_of(xs) returns the largest value of a non-empty list with a loop, without max().
tests:
  - call: 'max_of([3, 9, 2])'
    expect: '9'
  - call: 'max_of([-5])'
    expect: '-5'
  - call: 'max_of([-3, -1, -2])'
    expect: '-1'
  - call: 'max_of([7, 7, 1])'
    expect: '7'
---
def max_of(xs):
    best = xs[0]
    for x in xs:
        if x > best:
            best = x
    return best
