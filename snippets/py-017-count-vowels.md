---
id: py-017
title: Count vowels
lang: py
tags: [strings]
modes: [trace, recall, blank]
spec: Count the vowels (a, e, i, o, u in either case) in a string.
tests:
  - call: 'count_vowels("Programming")'
    expect: '3'
  - call: 'count_vowels("xyz")'
    expect: '0'
  - call: 'count_vowels("AEIOU")'
    expect: '5'
---
def count_vowels(s):
    return sum(c in "aeiouAEIOU" for c in s)
