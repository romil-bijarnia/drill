---
id: cs-055
title: Count a character
lang: cs
tags: [loops, strings]
family: count_char
modes: [trace, recall, blank]
spec: int CountChar(string s, char c) counts how many times c occurs in s with a loop, no LINQ.
tests:
  - call: 'CountChar("banana", 'a')'
    expect: '3'
  - call: 'CountChar("", 'x')'
    expect: '0'
  - call: 'CountChar("hello", 'l')'
    expect: '2'
  - call: 'CountChar("abc", 'z')'
    expect: '0'
---
public static int CountChar(string s, char c)
{
    int count = 0;
    foreach (var ch in s)
        if (ch == c) count++;
    return count;
}
