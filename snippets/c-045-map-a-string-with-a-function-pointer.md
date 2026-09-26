---
id: c-045
title: Map a string with a function pointer
lang: c
tags: [functions, strings]
modes: [trace, recall, blank]
spec: char *map_chars(char *s, int (*f)(int)) replaces every character c of s with f(c) in place and returns s; the tests pass toupper and tolower.
tests:
  - call: 'map_chars((char[]){"abc"}, toupper)'
    expect: 'ABC'
  - call: 'map_chars((char[]){"Hi There"}, tolower)'
    expect: 'hi there'
  - call: 'map_chars((char[]){""}, toupper)'
    expect: ''
  - call: 'map_chars((char[]){"a1!"}, toupper)'
    expect: 'A1!'
---
char *map_chars(char *s, int (*f)(int))
{
    for (char *p = s; *p; p++) *p = (char)f((unsigned char)*p);
    return s;
}
