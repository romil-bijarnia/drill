---
id: py-015
title: Group words by first letter
lang: py
tags: [dicts]
modes: [trace, recall, blank]
spec: Group words by first letter into a dict of lists, keeping input order.
tests:
  - call: 'group_by_initial(["apple", "avocado", "banana"])'
    expect: '{'a': ['apple', 'avocado'], 'b': ['banana']}'
  - call: 'group_by_initial([])'
    expect: '{}'
---
def group_by_initial(words):
    groups = {}
    for word in words:
        groups.setdefault(word[0], []).append(word)
    return groups
