---
id: c-038
title: Duplicate a string on the heap
lang: c
tags: [memory, strings]
modes: [trace, recall, blank]
spec: char *dup_string(const char *s) returns a malloc'd copy of s (terminator included) that the caller frees; NULL when malloc fails.
tests:
  - call: 'dup_string("hey")'
    expect: 'hey'
  - call: 'dup_string("")'
    expect: ''
  - call: '({ const char *s = "abc"; char *d = dup_string(s); d[0] = 'z'; int ok = d != s && s[0] == 'a' && d[1] == 'b'; free(d); ok; })'
    expect: '1'
  - call: '({ char *d = dup_string("four"); size_t n = strlen(d); free(d); n; })'
    expect: '4'
---
char *dup_string(const char *s)
{
    size_t n = strlen(s) + 1;
    char *copy = malloc(n);
    if (copy) memcpy(copy, s, n);
    return copy;
}
