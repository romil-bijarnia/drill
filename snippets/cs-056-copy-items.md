---
id: cs-056
title: Copy items
lang: cs
tags: [loops, arrays]
family: memcpy
modes: [trace, recall, blank]
spec: int[] CopyItems(int[] dst, int[] src, int n) copies the first n elements of src into dst by index with a loop and returns dst; no Array.Copy.
tests:
  - call: 'string.Join(",", CopyItems(new int[4], new[] {1, 2, 3}, 3))'
    expect: '1,2,3,0'
  - call: 'string.Join(",", CopyItems(new[] {9, 9, 9}, new[] {1, 2, 3}, 0))'
    expect: '9,9,9'
  - call: 'string.Join(",", CopyItems(new int[2], new[] {5, 6}, 2))'
    expect: '5,6'
  - call: 'string.Join(",", CopyItems(new int[3], new[] {7, 8, 9}, 1))'
    expect: '7,0,0'
---
public static int[] CopyItems(int[] dst, int[] src, int n)
{
    for (int i = 0; i < n; i++)
        dst[i] = src[i];
    return dst;
}
