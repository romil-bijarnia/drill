---
id: py-014
title: Balanced brackets
lang: py
tags: [data-structures, strings]
modes: [trace, recall, blank]
spec: Return True when every (, [ and { is closed by its match in the right order.
tests:
  - call: 'is_balanced("([]{})")'
    expect: 'True'
  - call: 'is_balanced("(]")'
    expect: 'False'
  - call: 'is_balanced("")'
    expect: 'True'
  - call: 'is_balanced("((")'
    expect: 'False'
---
def is_balanced(s):
    pairs = {")": "(", "]": "[", "}": "{"}
    stack = []
    for c in s:
        if c in "([{":
            stack.append(c)
        elif c in pairs:
            if not stack or stack.pop() != pairs[c]:
                return False
    return not stack
