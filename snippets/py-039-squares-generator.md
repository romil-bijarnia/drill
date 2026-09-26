---
id: py-039
title: Squares generator
lang: py
tags: [generators]
modes: [trace, recall, blank]
spec: A generator yielding the squares of 0 up to n-1.
tests:
  - call: 'list(squares(4))'
    expect: '[0, 1, 4, 9]'
  - call: 'list(squares(0))'
    expect: '[]'
---
def squares(n):
    for i in range(n):
        yield i * i
