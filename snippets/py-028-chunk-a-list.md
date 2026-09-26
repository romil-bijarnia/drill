---
id: py-028
title: Chunk a list
lang: py
tags: [generators]
modes: [trace, recall, blank]
spec: A generator yielding consecutive chunks of the given size; the last may be shorter.
tests:
  - call: 'list(chunk([1, 2, 3, 4, 5], 2))'
    expect: '[[1, 2], [3, 4], [5]]'
  - call: 'list(chunk([], 3))'
    expect: '[]'
---
def chunk(xs, size):
    for i in range(0, len(xs), size):
        yield xs[i:i + size]
