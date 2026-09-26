---
id: c-025
title: Sum between two pointers
lang: c
tags: [pointers, arrays]
modes: [trace, recall, blank]
spec: long sum_range(const int *begin, const int *end) adds the ints from begin up to but not including end, walking a pointer with no indexing.
tests:
  - call: '({ int a[] = {1, 2, 3, 4}; sum_range(a, a + 4); })'
    expect: '10'
  - call: '({ int a[] = {1, 2, 3, 4}; sum_range(a + 1, a + 3); })'
    expect: '5'
  - call: '({ int a[] = {5}; sum_range(a, a); })'
    expect: '0'
  - call: '({ int a[] = {-7}; sum_range(a, a + 1); })'
    expect: '-7'
---
long sum_range(const int *begin, const int *end)
{
    long total = 0;
    while (begin != end) total += *begin++;
    return total;
}
