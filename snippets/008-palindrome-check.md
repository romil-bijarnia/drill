---
id: 008
title: Palindrome
tags: [strings, two-pointers]
modes: [trace, recall, blank]
spec: Return true if the string reads the same backwards, ignoring case.
tests:
  - call: 'IsPalindrome("Level")'
    expect: 'True'
  - call: 'IsPalindrome("hello")'
    expect: 'False'
  - call: 'IsPalindrome("")'
    expect: 'True'
---
public static bool IsPalindrome(string s)
{
    int i = 0, j = s.Length - 1;
    while (i < j)
    {
        if (char.ToLowerInvariant(s[i]) != char.ToLowerInvariant(s[j])) return false;
        i++; j--;
    }
    return true;
}
