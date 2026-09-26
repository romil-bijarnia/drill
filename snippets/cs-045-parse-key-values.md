---
id: cs-045
title: Parse key=value pairs
tags: [strings, linq, collections]
modes: [trace, recall, blank]
spec: Parse "k=v;k2=v2" into a Dictionary<string, string>, ignoring empty segments.
tests:
  - call: 'ParseKv("a=1;b=2")["b"]'
    expect: '2'
  - call: 'ParseKv("").Count'
    expect: '0'
  - call: 'ParseKv("x=1;;y=2").Count'
    expect: '2'
---
public static Dictionary<string, string> ParseKv(string text) =>
    text.Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .ToDictionary(kv => kv[0], kv => kv[1]);
