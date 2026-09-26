---
id: py-033
title: Palindrome ignoring punctuation
lang: py
tags: [strings]
modes: [trace, recall, blank]
spec: True when the letters and digits of the string read the same both ways, ignoring case.
tests:
  - call: 'is_palindrome("A man, a plan, a canal: Panama")'
    expect: 'True'
  - call: 'is_palindrome("race a car")'
    expect: 'False'
  - call: 'is_palindrome("")'
    expect: 'True'
---
def is_palindrome(s):
    cleaned = [c.lower() for c in s if c.isalnum()]
    return cleaned == cleaned[::-1]
