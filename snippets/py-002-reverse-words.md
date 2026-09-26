---
id: py-002
title: Reverse words
lang: py
tags: [strings]
modes: [trace, recall, blank]
spec: Reverse the order of the words in a sentence.
tests:
  - call: 'reverse_words("hello big world")'
    expect: 'world big hello'
  - call: 'reverse_words("one")'
    expect: 'one'
  - call: 'reverse_words("")'
    expect: ''
---
def reverse_words(sentence):
    return " ".join(reversed(sentence.split()))
