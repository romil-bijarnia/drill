---
id: c-048
title: Set and clear a bit
lang: c
tags: [bits, basics]
modes: [trace, recall, blank]
spec: unsigned set_bit(unsigned v, int i) returns v with bit i set; unsigned clear_bit(unsigned v, int i) returns v with bit i cleared (0 <= i < 32).
tests:
  - call: 'set_bit(0, 3)'
    expect: '8'
  - call: 'set_bit(8, 3)'
    expect: '8'
  - call: 'set_bit(0, 31)'
    expect: '2147483648'
  - call: 'clear_bit(15, 0)'
    expect: '14'
  - call: 'clear_bit(0, 5)'
    expect: '0'
---
unsigned set_bit(unsigned v, int i)
{
    return v | (1u << i);
}

unsigned clear_bit(unsigned v, int i)
{
    return v & ~(1u << i);
}
