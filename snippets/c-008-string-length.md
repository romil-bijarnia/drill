---
id: c-008
title: String length
lang: c
tags: [strings, pointers]
modes: [trace, recall, blank]
spec: size_t my_strlen(const char *s) without calling strlen.
tests:
  - call: 'my_strlen("hello")'
    expect: '5'
  - call: 'my_strlen("")'
    expect: '0'
---
size_t my_strlen(const char *s)
{
    const char *p = s;
    while (*p) p++;
    return (size_t)(p - s);
}
