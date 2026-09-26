---
id: cs-004
title: Switch expression on a shape
tags: [pattern-matching]
modes: [trace, recall, blank]
spec: Given a side count, return "triangle", "square", "pentagon", or "polygon".
tests:
  - call: 'Name(3)'
    expect: 'triangle'
  - call: 'Name(4)'
    expect: 'square'
  - call: 'Name(9)'
    expect: 'polygon'
---
public static string Name(int sides) => sides switch
{
    3 => "triangle",
    4 => "square",
    5 => "pentagon",
    _ => "polygon"
};
