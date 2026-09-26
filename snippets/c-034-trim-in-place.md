---
id: c-034
title: Trim in place
lang: c
tags: [strings, pointers]
modes: [trace, recall, blank]
spec: char *trim(char *s) terminates the string before its trailing spaces and returns a pointer past the leading spaces, inside s, in one pass without <string.h>.
tests:
  - call: 'trim((char[]){"  hi  "})'
    expect: 'hi'
  - call: 'trim((char[]){"a b "})'
    expect: 'a b'
  - call: 'trim((char[]){"   "})'
    expect: ''
  - call: 'trim((char[]){"x"})'
    expect: 'x'
---
char *trim(char *s)
{
    while (*s == ' ') s++;
    char *end = s;
    for (char *p = s; *p; p++)
        if (*p != ' ') end = p + 1;
    *end = '\0';
    return s;
}
