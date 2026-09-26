---
id: c-033
title: Starts with
lang: c
tags: [strings, basics]
modes: [trace, recall, blank]
spec: bool starts_with(const char *s, const char *prefix) is true when s begins with prefix; an empty prefix always matches.
tests:
  - call: 'starts_with("hello", "he")'
    expect: 'true'
  - call: 'starts_with("hello", "lo")'
    expect: 'false'
  - call: 'starts_with("hi", "")'
    expect: 'true'
  - call: 'starts_with("", "a")'
    expect: 'false'
  - call: 'starts_with("ab", "abc")'
    expect: 'false'
---
bool starts_with(const char *s, const char *prefix)
{
    while (*prefix)
        if (*s++ != *prefix++) return false;
    return true;
}
