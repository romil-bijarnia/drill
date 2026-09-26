---
id: c-032
title: String compare
lang: c
tags: [strings, pointers]
modes: [trace, recall, blank]
spec: int str_cmp(const char *a, const char *b) returns -1, 0 or 1 from the first differing byte compared as unsigned char (a shorter prefix sorts first), without <string.h>.
tests:
  - call: 'str_cmp("abc", "abc")'
    expect: '0'
  - call: 'str_cmp("abc", "abd")'
    expect: '-1'
  - call: 'str_cmp("b", "a")'
    expect: '1'
  - call: 'str_cmp("ab", "abc")'
    expect: '-1'
  - call: 'str_cmp("", "")'
    expect: '0'
---
int str_cmp(const char *a, const char *b)
{
    while (*a && *a == *b) {
        a++;
        b++;
    }
    unsigned char x = *a, y = *b;
    return (x > y) - (x < y);
}
