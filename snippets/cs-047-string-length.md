---
id: cs-047
title: String length
lang: cs
tags: [loops, strings]
family: strlen
modes: [trace, recall, blank]
spec: int StrLen(string s) counts the characters with a loop, without .Length or LINQ.
tests:
  - call: 'StrLen("hello")'
    expect: '5'
  - call: 'StrLen("")'
    expect: '0'
  - call: 'StrLen("a b c")'
    expect: '5'
---
public static int StrLen(string s)
{
    int n = 0;
    foreach (char c in s) n++;
    return n;
}
