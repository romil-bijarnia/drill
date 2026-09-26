---
id: py-038
title: Format a table row
lang: py
tags: [strings, formatting]
modes: [trace, recall, blank]
spec: Name left-aligned in 8 columns followed by the score right-aligned in 3.
tests:
  - call: 'format_row("Ana", 7)'
    expect: 'Ana       7'
  - call: 'format_row("Benedict", 100)'
    expect: 'Benedict100'
---
def format_row(name, score):
    return f"{name:<8}{score:>3}"
