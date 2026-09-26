---
id: cs-012
title: Reverse words
tags: [strings]
modes: [trace, recall, blank]
spec: Reverse the order of the words in a sentence; words are separated by spaces.
tests:
  - call: 'ReverseWords("hello big world")'
    expect: 'world big hello'
  - call: 'ReverseWords("one")'
    expect: 'one'
  - call: 'ReverseWords("")'
    expect: ''
---
public static string ReverseWords(string sentence)
{
    var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    Array.Reverse(words);
    return string.Join(" ", words);
}
