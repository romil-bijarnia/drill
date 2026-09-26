---
id: py-018
title: Roman numerals
lang: py
tags: [algorithms, strings]
modes: [trace, recall, blank]
spec: Convert a positive integer below 4000 to Roman numerals.
tests:
  - call: 'to_roman(1994)'
    expect: 'MCMXCIV'
  - call: 'to_roman(4)'
    expect: 'IV'
  - call: 'to_roman(58)'
    expect: 'LVIII'
---
def to_roman(n):
    table = [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
             (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")]
    out = []
    for value, symbol in table:
        while n >= value:
            out.append(symbol)
            n -= value
    return "".join(out)
