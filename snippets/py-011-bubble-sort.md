---
id: py-011
title: Bubble sort
lang: py
tags: [algorithms]
modes: [trace, recall, blank]
spec: Sort a list in place with bubble sort and return it.
tests:
  - call: 'bubble_sort([3, 1, 2])'
    expect: '[1, 2, 3]'
  - call: 'bubble_sort([5, 4, 3, 2, 1])'
    expect: '[1, 2, 3, 4, 5]'
  - call: 'bubble_sort([])'
    expect: '[]'
---
def bubble_sort(xs):
    for end in range(len(xs) - 1, 0, -1):
        for i in range(end):
            if xs[i] > xs[i + 1]:
                xs[i], xs[i + 1] = xs[i + 1], xs[i]
    return xs
