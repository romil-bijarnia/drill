---
id: c-010
title: Count a character
lang: c
tags: [strings]
modes: [trace, recall, blank]
spec: int count_char(const char *s, char c) counts occurrences of c.
tests:
  - call: 'count_char("banana", 'a')'
    expect: '3'
  - call: 'count_char("", 'x')'
    expect: '0'
---
int count_char(const char *s, char c)
{
    int count = 0;
    for (; *s; s++)
        if (*s == c) count++;
    return count;
}
