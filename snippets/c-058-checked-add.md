---
id: c-058
title: Checked add
lang: c
tags: [basics, pointers, bits]
modes: [trace, recall, blank]
spec: bool add_checked(int a, int b, int *out) stores a + b in *out and returns true, or returns false and leaves *out alone when the sum would overflow int; compare against INT_MAX - b or INT_MIN - b instead of using a wider type.
tests:
  - call: '({ int r = 0; add_checked(2, 3, &r) ? r : -1; })'
    expect: '5'
  - call: '({ int r = 0; add_checked(INT_MAX, 1, &r); })'
    expect: 'false'
  - call: '({ int r = 0; add_checked(INT_MIN, -1, &r); })'
    expect: 'false'
  - call: '({ int r = 99; add_checked(INT_MAX, 1, &r); r; })'
    expect: '99'
  - call: '({ int r = 0; add_checked(INT_MIN, 0, &r) ? r : 1; })'
    expect: '-2147483648'
---
bool add_checked(int a, int b, int *out)
{
    if (b > 0 ? a > INT_MAX - b : a < INT_MIN - b) return false;
    *out = a + b;
    return true;
}
