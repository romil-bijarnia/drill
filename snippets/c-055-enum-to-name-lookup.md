---
id: c-055
title: Enum to name lookup
lang: c
tags: [basics, arrays]
modes: [trace, recall, blank]
spec: typedef enum { RED, GREEN, BLUE, COLOR_COUNT } Color; const char *color_name(Color c) returns "red", "green" or "blue" from a static table indexed by the enum, and "unknown" for any other value, negative included.
tests:
  - call: 'color_name(GREEN)'
    expect: 'green'
  - call: 'color_name(0)'
    expect: 'red'
  - call: 'color_name(BLUE)'
    expect: 'blue'
  - call: 'color_name(COLOR_COUNT)'
    expect: 'unknown'
  - call: 'color_name(-1)'
    expect: 'unknown'
---
typedef enum { RED, GREEN, BLUE, COLOR_COUNT } Color;

const char *color_name(Color c)
{
    static const char *const names[COLOR_COUNT] = {"red", "green", "blue"};
    return (unsigned)c < COLOR_COUNT ? names[c] : "unknown";
}
