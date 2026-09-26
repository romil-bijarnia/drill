---
id: c-050
title: Reverse the bits of a byte
lang: c
tags: [bits]
modes: [trace, recall, blank]
spec: unsigned char reverse_byte(unsigned char b) returns b with its 8 bits in the opposite order, using a loop.
tests:
  - call: 'reverse_byte(1)'
    expect: '128'
  - call: 'reverse_byte(0xF0)'
    expect: '15'
  - call: 'reverse_byte(0xAA)'
    expect: '85'
  - call: 'reverse_byte(0)'
    expect: '0'
  - call: 'reverse_byte(255)'
    expect: '255'
---
unsigned char reverse_byte(unsigned char b)
{
    unsigned char r = 0;
    for (int i = 0; i < 8; i++) {
        r = (unsigned char)((r << 1) | (b & 1));
        b >>= 1;
    }
    return r;
}
