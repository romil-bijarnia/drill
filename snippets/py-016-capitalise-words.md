---
id: py-016
title: Capitalise words
lang: py
tags: [strings]
modes: [trace, recall, blank]
spec: Upper-case the first letter of every space-separated word.
tests:
  - call: 'capitalize_words("hello big world")'
    expect: 'Hello Big World'
  - call: 'capitalize_words("")'
    expect: ''
  - call: 'capitalize_words("a")'
    expect: 'A'
---
def capitalize_words(text):
    return " ".join(w[:1].upper() + w[1:] for w in text.split(" "))
