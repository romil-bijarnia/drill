---
id: cs-030
title: Try-parse pattern
tags: [basics]
modes: [trace, recall, blank]
spec: TryParseAge(string, out int age) returns true only for a whole number from 0 to 150, and sets age to 0 otherwise.
tests:
  - call: 'TryParseAge("42", out var a) ? a : -1'
    expect: '42'
  - call: 'TryParseAge("abc", out var b) ? b : -1'
    expect: '-1'
  - call: 'TryParseAge("200", out var c) ? c : -1'
    expect: '-1'
---
public static bool TryParseAge(string text, out int age)
{
    if (int.TryParse(text, out age) && age >= 0 && age <= 150) return true;
    age = 0;
    return false;
}
