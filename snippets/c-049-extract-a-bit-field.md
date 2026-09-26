---
id: c-049
title: Extract a bit field
lang: c
tags: [bits]
modes: [trace, recall, blank]
spec: unsigned bit_field(unsigned v, int lo, int len) returns the len bits of v that start at bit lo, shifted down to the bottom (0 <= len < 32).
tests:
  - call: 'bit_field(0xF0, 4, 4)'
    expect: '15'
  - call: 'bit_field(0xABCD, 8, 8)'
    expect: '171'
  - call: 'bit_field(7, 1, 2)'
    expect: '3'
  - call: 'bit_field(255, 3, 0)'
    expect: '0'
  - call: 'bit_field(0x80000000u, 31, 1)'
    expect: '1'
---
unsigned bit_field(unsigned v, int lo, int len)
{
    return (v >> lo) & ((1u << len) - 1);
}
