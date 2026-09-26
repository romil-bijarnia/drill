---
id: py-003
title: Anagram check
lang: py
tags: [strings]
modes: [trace, recall, blank]
spec: Return True when two words are anagrams, ignoring case.
tests:
  - call: 'is_anagram("listen", "silent")'
    expect: 'True'
  - call: 'is_anagram("Dusty", "Study")'
    expect: 'True'
  - call: 'is_anagram("apple", "pale")'
    expect: 'False'
---
def is_anagram(a, b):
    return sorted(a.lower()) == sorted(b.lower())
