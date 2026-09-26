---
id: cs-041
title: Days between dates
tags: [dates]
modes: [trace, recall, blank]
spec: Number of days from one DateOnly to another; negative when the second is earlier.
tests:
  - call: 'DaysUntil(new DateOnly(2026, 9, 26), new DateOnly(2026, 10, 2))'
    expect: '6'
  - call: 'DaysUntil(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 1))'
    expect: '-1'
---
public static int DaysUntil(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber;
