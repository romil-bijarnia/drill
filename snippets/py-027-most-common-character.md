---
id: py-027
title: Most common character
lang: py
tags: [strings, dicts]
modes: [trace, recall, blank]
spec: The character that appears most often in a non-empty string; ties go to the one that appears first.
tests:
  - call: 'most_common("mississippi")'
    expect: 'i'
  - call: 'most_common("aab")'
    expect: 'a'
  - call: 'most_common("z")'
    expect: 'z'
---
def most_common(s):
    counts = {}
    for c in s:
        counts[c] = counts.get(c, 0) + 1
    best = s[0]
    for c in s:
        if counts[c] > counts[best]:
            best = c
    return best
