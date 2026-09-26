---
id: py-024
title: Transpose a matrix
lang: py
tags: [lists]
modes: [trace, recall, blank]
spec: Transpose a rectangular list of lists.
tests:
  - call: 'transpose([[1, 2, 3], [4, 5, 6]])'
    expect: '[[1, 4], [2, 5], [3, 6]]'
  - call: 'transpose([])'
    expect: '[]'
---
def transpose(m):
    return [list(row) for row in zip(*m)]
