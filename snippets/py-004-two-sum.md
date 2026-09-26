---
id: py-004
title: Two sum
lang: py
tags: [algorithms, dicts]
modes: [trace, recall, blank]
spec: Return the indices of the two numbers adding up to target as a list, lower index first, or an empty list.
tests:
  - call: 'two_sum([2, 7, 11, 15], 9)'
    expect: '[0, 1]'
  - call: 'two_sum([3, 2, 4], 6)'
    expect: '[1, 2]'
  - call: 'two_sum([1, 2], 5)'
    expect: '[]'
---
def two_sum(nums, target):
    seen = {}
    for i, n in enumerate(nums):
        if target - n in seen:
            return [seen[target - n], i]
        seen[n] = i
    return []
