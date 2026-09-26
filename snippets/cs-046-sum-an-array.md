---
id: cs-046
title: Sum an array
lang: cs
tags: [loops, arrays]
family: sum
modes: [trace, recall, blank]
spec: long SumArray(int[] xs) adds up the elements with a loop, no LINQ; an empty array gives 0.
tests:
  - call: 'SumArray(new[] {1, 2, 3, 4})'
    expect: '10'
  - call: 'SumArray(new int[0])'
    expect: '0'
  - call: 'SumArray(new[] {-5, 5, 7})'
    expect: '7'
---
public static long SumArray(int[] xs)
{
    long total = 0;
    foreach (var x in xs) total += x;
    return total;
}
