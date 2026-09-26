---
id: 037
title: Most common character
tags: [strings, collections]
modes: [trace, recall, blank]
spec: The character that appears most often in a non-empty string; ties go to the one that appears first.
tests:
  - call: 'MostCommon("mississippi")'
    expect: 'i'
  - call: 'MostCommon("aab")'
    expect: 'a'
  - call: 'MostCommon("z")'
    expect: 'z'
---
public static char MostCommon(string s)
{
    var counts = new Dictionary<char, int>();
    foreach (var c in s) counts[c] = counts.GetValueOrDefault(c) + 1;
    var best = s[0];
    foreach (var c in s)
        if (counts[c] > counts[best]) best = c;
    return best;
}
