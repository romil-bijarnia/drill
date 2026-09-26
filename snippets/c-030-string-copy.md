---
id: c-030
title: String copy
lang: c
tags: [strings, pointers]
modes: [trace, recall, blank]
spec: char *str_copy(char *dst, const char *src) copies src including its terminator into dst and returns dst, without <string.h>.
tests:
  - call: 'str_copy((char[8]){0}, "hey")'
    expect: 'hey'
  - call: 'str_copy((char[8]){"zzzzz"}, "ab")'
    expect: 'ab'
  - call: 'str_copy((char[8]){"zzzzz"}, "")'
    expect: ''
  - call: 'str_copy((char[8]){0}, "a")'
    expect: 'a'
---
char *str_copy(char *dst, const char *src)
{
    char *p = dst;
    while ((*p++ = *src++) != '\0')
        ;
    return dst;
}
