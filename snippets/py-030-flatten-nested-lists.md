---
id: py-030
title: Flatten nested lists
lang: py
tags: [recursion, lists]
modes: [trace, recall, blank]
spec: Flatten arbitrarily nested lists into one flat list.
tests:
  - call: 'flatten([1, [2, [3, 4]], 5])'
    expect: '[1, 2, 3, 4, 5]'
  - call: 'flatten([])'
    expect: '[]'
  - call: 'flatten([[[]]])'
    expect: '[]'
---
def flatten(xs):
    out = []
    for x in xs:
        if isinstance(x, list):
            out.extend(flatten(x))
        else:
            out.append(x)
    return out
