---
id: py-031
title: Word count
lang: py
tags: [dicts, strings]
modes: [trace, recall, blank]
spec: Count words in a string, case-insensitive, split on whitespace, as a dict in first-seen order.
tests:
  - call: 'word_count("a b A")'
    expect: '{'a': 2, 'b': 1}'
  - call: 'word_count("")'
    expect: '{}'
---
def word_count(text):
    counts = {}
    for word in text.lower().split():
        counts[word] = counts.get(word, 0) + 1
    return counts
