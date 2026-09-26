---
id: py-037
title: Common elements
lang: py
tags: [sets]
modes: [trace, recall, blank]
spec: Sorted list of the values that appear in both lists.
tests:
  - call: 'common([1, 2, 3, 3], [3, 2, 9])'
    expect: '[2, 3]'
  - call: 'common([], [1])'
    expect: '[]'
---
def common(a, b):
    return sorted(set(a) & set(b))
