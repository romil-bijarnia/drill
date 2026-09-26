---
id: 027
title: Count vowels
tags: [strings, linq]
modes: [trace, recall, blank]
spec: Count the vowels (a, e, i, o, u in either case) in a string.
tests:
  - call: 'CountVowels("Programming")'
    expect: '3'
  - call: 'CountVowels("xyz")'
    expect: '0'
  - call: 'CountVowels("AEIOU")'
    expect: '5'
---
public static int CountVowels(string s) => s.Count(c => "aeiouAEIOU".Contains(c));
