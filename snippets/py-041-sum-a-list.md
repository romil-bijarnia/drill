---
id: py-041
title: Sum a list
lang: py
tags: [loops, lists]
family: sum
modes: [trace, recall, blank]
spec: sum_list(xs) adds up the numbers in a list with a loop, without sum(); an empty list gives 0.
tests:
  - call: 'sum_list([1, 2, 3, 4])'
    expect: '10'
  - call: 'sum_list([])'
    expect: '0'
  - call: 'sum_list([-5, 5, 7])'
    expect: '7'
---
def sum_list(xs):
    total = 0
    for x in xs:
        total += x
    return total
