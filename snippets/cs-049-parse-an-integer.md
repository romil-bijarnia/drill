---
id: cs-049
title: Parse an integer
lang: cs
tags: [strings, arithmetic]
family: atoi
modes: [trace, recall, blank]
spec: long ParseInt(string s) turns a string of decimal digits with an optional leading minus into a long, digit by digit; no int.Parse, long.Parse or Convert.
tests:
  - call: 'ParseInt("42")'
    expect: '42'
  - call: 'ParseInt("-17")'
    expect: '-17'
  - call: 'ParseInt("0")'
    expect: '0'
  - call: 'ParseInt("1234567890")'
    expect: '1234567890'
---
public static long ParseInt(string s)
{
    int i = 0;
    bool negative = s[0] == '-';
    if (negative) i = 1;
    long value = 0;
    for (; i < s.Length; i++)
        value = value * 10 + (s[i] - '0');
    return negative ? -value : value;
}
