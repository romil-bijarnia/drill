---
id: c-024
title: Safe divide with an out parameter
lang: c
tags: [pointers]
modes: [trace, recall, blank]
spec: bool divide(int a, int b, int *out): false and untouched *out when b is 0.
tests:
  - call: '({ int r = -1; divide(10, 2, &r) ? r : -99; })'
    expect: '5'
  - call: '({ int r = -1; divide(1, 0, &r) ? r : -99; })'
    expect: '-99'
---
bool divide(int a, int b, int *out)
{
    if (b == 0) return false;
    *out = a / b;
    return true;
}
