---
id: cs-020
title: Merge two sorted arrays
tags: [algorithms]
modes: [trace, recall, blank]
spec: Merge two ascending arrays into one ascending list without calling a sort.
tests:
  - call: 'string.Join(",", Merge(new[] {1, 3, 5}, new[] {2, 4}))'
    expect: '1,2,3,4,5'
  - call: 'string.Join(",", Merge(new int[0], new[] {1}))'
    expect: '1'
  - call: 'string.Join(",", Merge(new int[0], new int[0]))'
    expect: ''
---
public static List<int> Merge(int[] a, int[] b)
{
    var result = new List<int>(a.Length + b.Length);
    int i = 0, j = 0;
    while (i < a.Length && j < b.Length)
        result.Add(a[i] <= b[j] ? a[i++] : b[j++]);
    while (i < a.Length) result.Add(a[i++]);
    while (j < b.Length) result.Add(b[j++]);
    return result;
}
