---
id: cs-043
title: Type pattern on object
tags: [pattern-matching]
modes: [trace, recall, blank]
spec: Describe an object: "int N" for ints, "text S" for strings, "null" for null, "other" for anything else.
tests:
  - call: 'Describe(5)'
    expect: 'int 5'
  - call: 'Describe("hi")'
    expect: 'text hi'
  - call: 'Describe(null)'
    expect: 'null'
  - call: 'Describe(2.5)'
    expect: 'other'
---
public static string Describe(object value) => value switch
{
    int n => $"int {n}",
    string s => $"text {s}",
    null => "null",
    _ => "other"
};
