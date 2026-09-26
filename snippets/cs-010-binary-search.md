---
id: cs-010
title: Binary search
tags: [algorithms, search]
modes: [trace, recall, blank]
spec: Return the index of target in a sorted array, or -1 if absent.
tests:
  - call: 'Search(new[] {1, 3, 5, 7, 9}, 7)'
    expect: '3'
  - call: 'Search(new[] {1, 3, 5, 7, 9}, 4)'
    expect: '-1'
  - call: 'Search(new int[0], 1)'
    expect: '-1'
---
public static int Search(int[] xs, int target)
{
    int lo = 0, hi = xs.Length - 1;
    while (lo <= hi)
    {
        int mid = lo + (hi - lo) / 2;
        if (xs[mid] == target) return mid;
        if (xs[mid] < target) lo = mid + 1;
        else hi = mid - 1;
    }
    return -1;
}
