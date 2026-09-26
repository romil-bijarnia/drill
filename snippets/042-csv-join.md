---
id: 042
title: Join rows as CSV
tags: [strings, linq]
modes: [trace, recall, blank]
spec: Join rows of strings into CSV text: commas between fields, newlines between rows.
tests:
  - call: 'Csv(new[] { new[] {"a", "b"}, new[] {"1", "2"} }).Replace("\n", "/")'
    expect: 'a,b/1,2'
  - call: 'Csv(new string[0][])'
    expect: ''
---
public static string Csv(IEnumerable<string[]> rows) =>
    string.Join("\n", rows.Select(r => string.Join(",", r)));
