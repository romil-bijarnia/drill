---
id: c-061
title: Parse an integer
lang: c
tags: [strings, pointers]
family: atoi
modes: [trace, recall, blank]
spec: long parse_int(const char *s) turns a string of decimal digits with an optional leading minus into a long, digit by digit; no atoi or strtol.
tests:
  - call: 'parse_int("42")'
    expect: '42'
  - call: 'parse_int("-17")'
    expect: '-17'
  - call: 'parse_int("0")'
    expect: '0'
  - call: 'parse_int("1234567890")'
    expect: '1234567890'
---
long parse_int(const char *s)
{
    bool negative = *s == '-';
    if (negative) s++;
    long value = 0;
    for (; *s; s++)
        value = value * 10 + (*s - '0');
    return negative ? -value : value;
}
