---
id: c-016
title: Upper-case in place
lang: c
tags: [strings]
modes: [trace, recall, blank]
spec: char *upper(char *s) upper-cases every letter in place and returns s.
tests:
  - call: 'upper((char[]){"abc1"})'
    expect: 'ABC1'
  - call: 'upper((char[]){""})'
    expect: ''
---
char *upper(char *s)
{
    for (char *p = s; *p; p++) *p = (char)toupper((unsigned char)*p);
    return s;
}
