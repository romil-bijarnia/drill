---
id: cs-013
title: Anagram check
tags: [strings, linq]
modes: [trace, recall, blank]
spec: Return true when the two words are anagrams of each other, ignoring case.
tests:
  - call: 'IsAnagram("listen", "silent")'
    expect: 'True'
  - call: 'IsAnagram("Dusty", "Study")'
    expect: 'True'
  - call: 'IsAnagram("apple", "pale")'
    expect: 'False'
---
public static bool IsAnagram(string a, string b)
{
    static string Key(string s) => string.Concat(s.ToLowerInvariant().OrderBy(c => c));
    return Key(a) == Key(b);
}
