---
id: 033
title: Invert a dictionary
tags: [collections, linq]
modes: [trace, recall, blank]
spec: Swap the keys and values of a Dictionary<string, int> whose values are unique.
tests:
  - call: 'Invert(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 })[2]'
    expect: 'b'
  - call: 'Invert(new Dictionary<string, int>()).Count'
    expect: '0'
---
public static Dictionary<int, string> Invert(Dictionary<string, int> source) =>
    source.ToDictionary(kv => kv.Value, kv => kv.Key);
