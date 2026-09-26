---
id: cs-048
title: Reverse a string
lang: cs
tags: [loops, strings]
family: reverse
modes: [trace, recall, blank]
spec: string Reverse(string s) returns the characters in reverse order by filling a char array in a loop; no Array.Reverse and no LINQ.
tests:
  - call: 'Reverse("hello")'
    expect: 'olleh'
  - call: 'Reverse("")'
    expect: ''
  - call: 'Reverse("ab")'
    expect: 'ba'
  - call: 'Reverse("abc")'
    expect: 'cba'
---
public static string Reverse(string s)
{
    var chars = new char[s.Length];
    for (int i = 0; i < s.Length; i++)
        chars[i] = s[s.Length - 1 - i];
    return new string(chars);
}
