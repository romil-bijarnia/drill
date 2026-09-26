---
id: py-043
title: Reverse a string
lang: py
tags: [loops, strings]
family: reverse
modes: [trace, recall, blank]
spec: reverse(s) returns the characters of s in reverse order, built up with a loop; no slicing and no reversed().
tests:
  - call: 'reverse("hello")'
    expect: 'olleh'
  - call: 'reverse("")'
    expect: ''
  - call: 'reverse("ab")'
    expect: 'ba'
  - call: 'reverse("abc")'
    expect: 'cba'
---
def reverse(s):
    out = ""
    for c in s:
        out = c + out
    return out
