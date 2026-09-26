---
id: 022
title: Quicksort
tags: [algorithms, recursion, linq]
modes: [trace, recall, blank]
spec: Sort a list with recursive quicksort: pick a pivot, partition into smaller, equal and larger, recurse.
tests:
  - call: 'string.Join(",", QuickSort(new List<int> {5, 3, 8, 1}))'
    expect: '1,3,5,8'
  - call: 'string.Join(",", QuickSort(new List<int>()))'
    expect: ''
  - call: 'string.Join(",", QuickSort(new List<int> {2, 2, 1}))'
    expect: '1,2,2'
---
public static List<int> QuickSort(List<int> xs)
{
    if (xs.Count <= 1) return xs;
    var pivot = xs[xs.Count / 2];
    var less = xs.Where(x => x < pivot).ToList();
    var equal = xs.Where(x => x == pivot).ToList();
    var greater = xs.Where(x => x > pivot).ToList();
    return QuickSort(less).Concat(equal).Concat(QuickSort(greater)).ToList();
}
