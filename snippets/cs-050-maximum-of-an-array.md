---
id: cs-050
title: Maximum of an array
lang: cs
tags: [loops, arrays]
family: max
modes: [trace, recall, blank]
spec: int MaxOf(int[] xs) returns the largest element of a non-empty array with a loop, no LINQ.
tests:
  - call: 'MaxOf(new[] {3, 9, 2})'
    expect: '9'
  - call: 'MaxOf(new[] {-5})'
    expect: '-5'
  - call: 'MaxOf(new[] {-3, -1, -2})'
    expect: '-1'
  - call: 'MaxOf(new[] {7, 7, 1})'
    expect: '7'
---
public static int MaxOf(int[] xs)
{
    int best = xs[0];
    foreach (var x in xs)
        if (x > best) best = x;
    return best;
}
