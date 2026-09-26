---
id: cs-021
title: Bubble sort
tags: [algorithms]
modes: [trace, recall, blank]
spec: Sort an array in place with bubble sort and return it.
tests:
  - call: 'string.Join(",", BubbleSort(new[] {3, 1, 2}))'
    expect: '1,2,3'
  - call: 'string.Join(",", BubbleSort(new[] {5, 4, 3, 2, 1}))'
    expect: '1,2,3,4,5'
  - call: 'string.Join(",", BubbleSort(new int[0]))'
    expect: ''
---
public static int[] BubbleSort(int[] xs)
{
    for (int pass = 0; pass < xs.Length - 1; pass++)
        for (int i = 0; i < xs.Length - 1 - pass; i++)
            if (xs[i] > xs[i + 1]) (xs[i], xs[i + 1]) = (xs[i + 1], xs[i]);
    return xs;
}
