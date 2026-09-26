---
id: c-057
title: djb2 hash
lang: c
tags: [strings, basics]
modes: [trace, recall, blank]
spec: unsigned long djb2(const char *s) starts at 5381 and for each byte c does hash = hash * 33 + c, wrapping silently.
tests:
  - call: 'djb2("")'
    expect: '5381'
  - call: 'djb2("a")'
    expect: '177670'
  - call: 'djb2("abc")'
    expect: '193485963'
  - call: 'djb2("hello")'
    expect: '210714636441'
---
unsigned long djb2(const char *s)
{
    unsigned long hash = 5381;
    for (; *s; s++) hash = hash * 33 + (unsigned char)*s;
    return hash;
}
