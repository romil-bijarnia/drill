---
id: c-009
title: Reverse a string in place
lang: c
tags: [strings, pointers]
modes: [trace, recall, blank]
spec: char *reverse(char *s) reverses s in place and returns it.
tests:
  - call: 'reverse((char[]){"hello"})'
    expect: 'olleh'
  - call: 'reverse((char[]){""})'
    expect: ''
  - call: 'reverse((char[]){"ab"})'
    expect: 'ba'
---
char *reverse(char *s)
{
    size_t n = strlen(s);
    for (size_t i = 0; i < n / 2; i++) {
        char tmp = s[i];
        s[i] = s[n - 1 - i];
        s[n - 1 - i] = tmp;
    }
    return s;
}
