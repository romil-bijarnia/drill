---
id: py-010
title: Merge two sorted lists
lang: py
tags: [algorithms]
modes: [trace, recall, blank]
spec: Merge two ascending lists into one ascending list without calling sort.
tests:
  - call: 'merge([1, 3, 5], [2, 4])'
    expect: '[1, 2, 3, 4, 5]'
  - call: 'merge([], [1])'
    expect: '[1]'
  - call: 'merge([], [])'
    expect: '[]'
---
def merge(a, b):
    result = []
    i = j = 0
    while i < len(a) and j < len(b):
        if a[i] <= b[j]:
            result.append(a[i])
            i += 1
        else:
            result.append(b[j])
            j += 1
    result.extend(a[i:])
    result.extend(b[j:])
    return result
