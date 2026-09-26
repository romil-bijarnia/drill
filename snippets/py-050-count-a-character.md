---
id: py-050
title: Count a character
lang: py
tags: [loops, strings]
family: count_char
modes: [trace, recall, blank]
spec: count_char(s, c) counts how many times the character c occurs in s with a loop, without .count().
tests:
  - call: 'count_char("banana", "a")'
    expect: '3'
  - call: 'count_char("", "x")'
    expect: '0'
  - call: 'count_char("hello", "l")'
    expect: '2'
  - call: 'count_char("abc", "z")'
    expect: '0'
---
def count_char(s, c):
    count = 0
    for ch in s:
        if ch == c:
            count += 1
    return count
