---
id: py-048
title: Insertion sort
lang: py
tags: [algorithms, lists]
family: isort
modes: [trace, recall, blank]
spec: insertion_sort(xs) sorts the list in place with insertion sort and returns the same list; no sort() or sorted().
tests:
  - call: 'insertion_sort([3, 1, 2])'
    expect: '[1, 2, 3]'
  - call: 'insertion_sort([5, 4, 3, 2, 1])'
    expect: '[1, 2, 3, 4, 5]'
  - call: 'insertion_sort([])'
    expect: '[]'
  - call: 'insertion_sort([7])'
    expect: '[7]'
---
def insertion_sort(xs):
    for i in range(1, len(xs)):
        key = xs[i]
        j = i - 1
        while j >= 0 and xs[j] > key:
            xs[j + 1] = xs[j]
            j -= 1
        xs[j + 1] = key
    return xs
