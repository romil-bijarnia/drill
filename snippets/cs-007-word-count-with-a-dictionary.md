---
id: cs-007
title: Word frequency
tags: [collections, strings]
modes: [trace, recall, blank]
spec: Count words in a string, case-insensitive, split on spaces.
tests:
  - call: 'WordCount("a b A")["a"]'
    expect: '2'
  - call: 'WordCount("x y z").Count'
    expect: '3'
  - call: 'WordCount("").Count'
    expect: '0'
---
public static Dictionary<string, int> WordCount(string text)
{
    var counts = new Dictionary<string, int>();
    foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
        var key = word.ToLowerInvariant();
        counts[key] = counts.GetValueOrDefault(key) + 1;
    }
    return counts;
}
