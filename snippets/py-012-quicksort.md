---
id: py-012
title: Quicksort
lang: py
tags: [algorithms, recursion]
modes: [trace, recall, blank]
spec: Sort a list with recursive quicksort using comprehensions for the partitions.
tests:
  - call: 'quicksort([5, 3, 8, 1])'
    expect: '[1, 3, 5, 8]'
  - call: 'quicksort([])'
    expect: '[]'
  - call: 'quicksort([2, 2, 1])'
    expect: '[1, 2, 2]'
---
def quicksort(xs):
    if len(xs) <= 1:
        return xs
    pivot = xs[len(xs) // 2]
    less = [x for x in xs if x < pivot]
    equal = [x for x in xs if x == pivot]
    greater = [x for x in xs if x > pivot]
    return quicksort(less) + equal + quicksort(greater)
