---
id: c-019
title: Count words
lang: c
tags: [strings]
modes: [trace, recall, blank]
spec: int count_words(const char *s) counts runs of non-space characters.
tests:
  - call: 'count_words("  hello   big world ")'
    expect: '3'
  - call: 'count_words("")'
    expect: '0'
  - call: 'count_words("one")'
    expect: '1'
---
int count_words(const char *s)
{
    int count = 0;
    bool in_word = false;
    for (; *s; s++) {
        if (isspace((unsigned char)*s)) {
            in_word = false;
        } else if (!in_word) {
            in_word = true;
            count++;
        }
    }
    return count;
}
