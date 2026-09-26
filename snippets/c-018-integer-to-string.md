---
id: c-018
title: Integer to string
lang: c
tags: [strings]
modes: [trace, recall, blank]
spec: char *to_string(int n, char *buf) writes n in decimal into buf (at least 12 bytes) and returns buf.
tests:
  - call: 'to_string(-42, (char[16]){0})'
    expect: '-42'
  - call: 'to_string(0, (char[16]){0})'
    expect: '0'
  - call: 'to_string(1234, (char[16]){0})'
    expect: '1234'
---
char *to_string(int n, char *buf)
{
    char tmp[16];
    int i = 0;
    bool negative = n < 0;
    long value = negative ? -(long)n : n;
    do {
        tmp[i++] = (char)('0' + value % 10);
        value /= 10;
    } while (value > 0);
    int j = 0;
    if (negative) buf[j++] = '-';
    while (i > 0) buf[j++] = tmp[--i];
    buf[j] = '\0';
    return buf;
}
