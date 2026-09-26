---
id: cs-026
title: Capitalise words
tags: [strings, linq]
modes: [trace, recall, blank]
spec: Upper-case the first letter of every space-separated word.
tests:
  - call: 'Capitalize("hello big world")'
    expect: 'Hello Big World'
  - call: 'Capitalize("")'
    expect: ''
  - call: 'Capitalize("a")'
    expect: 'A'
---
public static string Capitalize(string text) =>
    string.Join(" ", text.Split(' ').Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));
