---
id: cs-053
title: Insertion sort
lang: cs
tags: [algorithms, arrays]
family: isort
modes: [trace, recall, blank]
spec: int[] InsertionSort(int[] xs) sorts the array in place with insertion sort and returns the same array; no Array.Sort or LINQ.
tests:
  - call: 'string.Join(",", InsertionSort(new[] {3, 1, 2}))'
    expect: '1,2,3'
  - call: 'string.Join(",", InsertionSort(new[] {5, 4, 3, 2, 1}))'
    expect: '1,2,3,4,5'
  - call: 'string.Join(",", InsertionSort(new int[0]))'
    expect: ''
  - call: 'string.Join(",", InsertionSort(new[] {7}))'
    expect: '7'
---
public static int[] InsertionSort(int[] xs)
{
    for (int i = 1; i < xs.Length; i++)
    {
        int key = xs[i];
        int j = i - 1;
        while (j >= 0 && xs[j] > key)
        {
            xs[j + 1] = xs[j];
            j--;
        }
        xs[j + 1] = key;
    }
    return xs;
}
