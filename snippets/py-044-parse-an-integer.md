---
id: py-044
title: Parse an integer
lang: py
tags: [strings, arithmetic]
family: atoi
modes: [trace, recall, blank]
spec: parse_int(s) turns a string of decimal digits with an optional leading minus into an int, digit by digit with ord(); no int().
tests:
  - call: 'parse_int("42")'
    expect: '42'
  - call: 'parse_int("-17")'
    expect: '-17'
  - call: 'parse_int("0")'
    expect: '0'
  - call: 'parse_int("1234567890")'
    expect: '1234567890'
---
def parse_int(s):
    i = 0
    negative = s[0] == "-"
    if negative:
        i = 1
    value = 0
    while i < len(s):
        value = value * 10 + ord(s[i]) - ord("0")
        i += 1
    return -value if negative else value
