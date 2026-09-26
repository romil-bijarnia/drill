---
id: py-042
title: String length
lang: py
tags: [loops, strings]
family: strlen
modes: [trace, recall, blank]
spec: str_len(s) counts the characters of a string with a loop, without len().
tests:
  - call: 'str_len("hello")'
    expect: '5'
  - call: 'str_len("")'
    expect: '0'
  - call: 'str_len("a b c")'
    expect: '5'
---
def str_len(s):
    n = 0
    for _ in s:
        n += 1
    return n
