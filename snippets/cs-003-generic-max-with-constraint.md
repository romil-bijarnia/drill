---
id: cs-003
title: Generic max with constraint
tags: [generics, constraints]
modes: [trace, recall, blank]
spec: Return the larger of two values of any comparable type.
tests:
  - call: 'Max(3, 7)'
    expect: '7'
  - call: 'Max("apple", "pear")'
    expect: 'pear'
  - call: 'Max(2.5, 2.5)'
    expect: '2.5'
---
public static T Max<T>(T a, T b) where T : IComparable<T>
    => a.CompareTo(b) >= 0 ? a : b;
