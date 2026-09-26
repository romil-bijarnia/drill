---
id: cs-002
title: Async delay and return
tags: [async]
modes: [trace, recall, blank]
spec: Asynchronously wait the given milliseconds, then return the input doubled.
tests:
  - call: 'DoubleLater(21, 1).Result'
    expect: '42'
  - call: 'DoubleLater(0, 1).Result'
    expect: '0'
---
public static async Task<int> DoubleLater(int x, int ms)
{
    await Task.Delay(ms);
    return x * 2;
}
