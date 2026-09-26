---
id: py-022
title: Average of the top three
lang: py
tags: [basics]
modes: [trace, recall, blank]
spec: Mean of the three largest values rounded to two decimals; 0 for an empty list.
tests:
  - call: 'top_average([1, 5, 3, 9, 7])'
    expect: '7.0'
  - call: 'top_average([4])'
    expect: '4.0'
  - call: 'top_average([])'
    expect: '0'
  - call: 'top_average([1, 2])'
    expect: '1.5'
---
def top_average(xs):
    top = sorted(xs, reverse=True)[:3]
    return round(sum(top) / len(top), 2) if top else 0
