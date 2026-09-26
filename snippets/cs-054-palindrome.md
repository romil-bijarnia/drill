---
id: cs-054
title: Palindrome
lang: cs
tags: [strings, two-pointers]
family: palindrome
modes: [trace, recall, blank]
spec: bool IsPalindrome(string s) is true when s reads the same backwards, comparing characters from both ends inwards; case matters and the empty string counts.
tests:
  - call: 'IsPalindrome("racecar")'
    expect: 'True'
  - call: 'IsPalindrome("hello")'
    expect: 'False'
  - call: 'IsPalindrome("abba")'
    expect: 'True'
  - call: 'IsPalindrome("")'
    expect: 'True'
---
public static bool IsPalindrome(string s)
{
    int i = 0, j = s.Length - 1;
    while (i < j)
    {
        if (s[i] != s[j]) return false;
        i++;
        j--;
    }
    return true;
}
