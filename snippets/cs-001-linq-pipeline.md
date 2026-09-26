---
id: cs-001
title: LINQ pipeline
tags: [linq]
modes: [trace, recall, blank]
spec: Given a list of integers, return the even ones squared, largest first.
tests:
  - call: 'string.Join(",", EvenSquares(new[] {1, 2, 3, 4}))'
    expect: '16,4'
  - call: 'string.Join(",", EvenSquares(new int[0]))'
    expect: ''
  - call: 'string.Join(",", EvenSquares(new[] {6, 2, 4}))'
    expect: '36,16,4'
---
public static IEnumerable<int> EvenSquares(IEnumerable<int> xs)
    => xs.Where(x => x % 2 == 0)
         .Select(x => x * x)
         .OrderByDescending(x => x);
