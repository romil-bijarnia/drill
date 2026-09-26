---
id: cs-023
title: Maximum subarray
tags: [algorithms]
modes: [trace, recall, blank]
spec: Largest sum of any contiguous, non-empty subarray (Kadane's algorithm).
tests:
  - call: 'MaxSubarray(new[] {-2, 1, -3, 4, -1, 2, 1, -5, 4})'
    expect: '6'
  - call: 'MaxSubarray(new[] {-1})'
    expect: '-1'
  - call: 'MaxSubarray(new[] {1, 2, 3})'
    expect: '6'
---
public static int MaxSubarray(int[] xs)
{
    int best = xs[0], current = xs[0];
    for (int i = 1; i < xs.Length; i++)
    {
        current = Math.Max(xs[i], current + xs[i]);
        best = Math.Max(best, current);
    }
    return best;
}
