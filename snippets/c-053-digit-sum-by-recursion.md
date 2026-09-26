---
id: c-053
title: Digit sum by recursion
lang: c
tags: [recursion, basics]
modes: [trace, recall, blank]
spec: int digit_sum(long n) adds the decimal digits of n recursively (last digit plus the sum of the rest); a negative n uses its absolute value.
tests:
  - call: 'digit_sum(1234)'
    expect: '10'
  - call: 'digit_sum(9)'
    expect: '9'
  - call: 'digit_sum(-99)'
    expect: '18'
  - call: 'digit_sum(0)'
    expect: '0'
  - call: 'digit_sum(1000000007)'
    expect: '8'
---
int digit_sum(long n)
{
    if (n < 0) return digit_sum(-n);
    if (n < 10) return (int)n;
    return (int)(n % 10) + digit_sum(n / 10);
}
