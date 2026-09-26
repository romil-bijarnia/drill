---
id: 040
title: Async sum
tags: [async]
modes: [trace, recall, blank]
spec: Sum a sequence on a background thread and return the total asynchronously.
tests:
  - call: 'SumAsync(new[] {1, 2, 3}).Result'
    expect: '6'
  - call: 'SumAsync(new int[0]).Result'
    expect: '0'
---
public static Task<int> SumAsync(IEnumerable<int> xs) => Task.Run(() => xs.Sum());
