---
id: py-020
title: Parse an age or None
lang: py
tags: [basics, exceptions]
modes: [trace, recall, blank]
spec: Return the integer for a whole-number string from 0 to 150, otherwise None.
tests:
  - call: 'parse_age("42")'
    expect: '42'
  - call: 'parse_age("abc")'
    expect: ''
  - call: 'parse_age("200")'
    expect: ''
---
def parse_age(text):
    try:
        age = int(text)
    except ValueError:
        return None
    return age if 0 <= age <= 150 else None
