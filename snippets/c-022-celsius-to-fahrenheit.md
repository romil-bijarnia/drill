---
id: c-022
title: Celsius to Fahrenheit
lang: c
tags: [basics]
modes: [trace, recall, blank]
spec: double to_fahrenheit(double c).
tests:
  - call: 'to_fahrenheit(100)'
    expect: '212'
  - call: 'to_fahrenheit(-40)'
    expect: '-40'
  - call: 'to_fahrenheit(37)'
    expect: '98.6'
---
double to_fahrenheit(double c)
{
    return c * 9.0 / 5.0 + 32.0;
}
