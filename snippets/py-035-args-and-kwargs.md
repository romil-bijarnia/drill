---
id: py-035
title: args and kwargs
lang: py
tags: [functions]
modes: [trace, recall, blank]
spec: Return "<n> args, <sorted kwarg names>" for any call, so describe(1, 2, x=1) gives "2 args, ['x']".
tests:
  - call: 'describe(1, 2, x=1)'
    expect: '2 args, ['x']'
  - call: 'describe()'
    expect: '0 args, []'
---
def describe(*args, **kwargs):
    return f"{len(args)} args, {sorted(kwargs)}"
