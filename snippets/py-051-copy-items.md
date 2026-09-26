---
id: py-051
title: Copy items
lang: py
tags: [loops, lists]
family: memcpy
modes: [trace, recall, blank]
spec: copy_items(dst, src, n) copies the first n items of src into dst by index with a loop and returns dst; no slicing.
tests:
  - call: 'copy_items([0, 0, 0, 0], [1, 2, 3], 3)'
    expect: '[1, 2, 3, 0]'
  - call: 'copy_items([9, 9, 9], [1, 2, 3], 0)'
    expect: '[9, 9, 9]'
  - call: 'copy_items([0, 0], [5, 6], 2)'
    expect: '[5, 6]'
  - call: 'copy_items([0, 0, 0], [7, 8, 9], 1)'
    expect: '[7, 0, 0]'
---
def copy_items(dst, src, n):
    for i in range(n):
        dst[i] = src[i]
    return dst
