---
id: c-063
title: Palindrome
lang: c
tags: [strings, two-pointers]
family: palindrome
modes: [trace, recall, blank]
spec: _Bool is_palindrome(const char *s) is true when s reads the same backwards, comparing characters from both ends inwards; case matters and the empty string counts.
tests:
  - call: 'is_palindrome("racecar")'
    expect: 'true'
  - call: 'is_palindrome("hello")'
    expect: 'false'
  - call: 'is_palindrome("abba")'
    expect: 'true'
  - call: 'is_palindrome("")'
    expect: 'true'
---
bool is_palindrome(const char *s)
{
    long i = 0, j = (long)strlen(s) - 1;
    while (i < j) {
        if (s[i] != s[j]) return false;
        i++;
        j--;
    }
    return true;
}
