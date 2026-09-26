---
id: 036
title: Memoised Fibonacci
tags: [recursion, collections]
modes: [trace, recall, blank]
spec: The nth Fibonacci number (F0 = 0, F1 = 1) as a long, memoised in a dictionary.
tests:
  - call: 'FibMemo(50)'
    expect: '12586269025'
  - call: 'FibMemo(0)'
    expect: '0'
  - call: 'FibMemo(10)'
    expect: '55'
---
static readonly Dictionary<int, long> Memo = new();

public static long FibMemo(int n)
{
    if (n < 2) return n;
    if (Memo.TryGetValue(n, out var cached)) return cached;
    return Memo[n] = FibMemo(n - 1) + FibMemo(n - 2);
}
