---
id: c-031
title: String concatenate
lang: c
tags: [strings, pointers]
modes: [trace, recall, blank]
spec: char *str_cat(char *dst, const char *src) appends src after the string already in dst and returns dst, without <string.h>.
tests:
  - call: 'str_cat((char[16]){"foo"}, "bar")'
    expect: 'foobar'
  - call: 'str_cat((char[16]){""}, "x")'
    expect: 'x'
  - call: 'str_cat((char[16]){"abc"}, "")'
    expect: 'abc'
  - call: 'str_cat(str_cat((char[16]){"a"}, "b"), "c")'
    expect: 'abc'
---
char *str_cat(char *dst, const char *src)
{
    char *p = dst;
    while (*p) p++;
    while ((*p++ = *src++) != '\0')
        ;
    return dst;
}
