---
id: py-023
title: Invert a dict
lang: py
tags: [dicts]
modes: [trace, recall, blank]
spec: Swap keys and values of a dict whose values are unique.
tests:
  - call: 'invert({"a": 1, "b": 2})[2]'
    expect: 'b'
  - call: 'len(invert({}))'
    expect: '0'
---
def invert(d):
    return {v: k for k, v in d.items()}
