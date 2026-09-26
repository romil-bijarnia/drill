---
id: c-064
title: Copy bytes
lang: c
tags: [pointers, memory]
family: memcpy
modes: [trace, recall, blank]
spec: char *copy_bytes(char *dst, const char *src, long n) copies n bytes from src to dst with a loop and returns dst; no memcpy.
tests:
  - call: 'copy_bytes((char[8]){0}, "hello", 6)'
    expect: 'hello'
  - call: 'copy_bytes((char[8]){0}, "hello", 3)'
    expect: 'hel'
  - call: 'copy_bytes((char[4]){"xyz"}, "abc", 0)'
    expect: 'xyz'
  - call: 'copy_bytes((char[4]){0}, "abc", 4)'
    expect: 'abc'
---
char *copy_bytes(char *dst, const char *src, long n)
{
    for (long i = 0; i < n; i++)
        dst[i] = src[i];
    return dst;
}
