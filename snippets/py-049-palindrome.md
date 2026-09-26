---
id: py-049
title: Palindrome
lang: py
tags: [strings, two-pointers]
family: palindrome
modes: [trace, recall, blank]
spec: is_palindrome(s) is True when s reads the same backwards, comparing characters from both ends inwards; case matters and the empty string counts.
tests:
  - call: 'is_palindrome("racecar")'
    expect: 'True'
  - call: 'is_palindrome("hello")'
    expect: 'False'
  - call: 'is_palindrome("abba")'
    expect: 'True'
  - call: 'is_palindrome("")'
    expect: 'True'
---
def is_palindrome(s):
    i, j = 0, len(s) - 1
    while i < j:
        if s[i] != s[j]:
            return False
        i += 1
        j -= 1
    return True
