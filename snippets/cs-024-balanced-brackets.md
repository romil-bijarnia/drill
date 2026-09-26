---
id: cs-024
title: Balanced brackets
tags: [data-structures, strings]
modes: [trace, recall, blank]
spec: Return true when every (, [ and { is closed by its matching bracket in the right order.
tests:
  - call: 'IsBalanced("([]{})")'
    expect: 'True'
  - call: 'IsBalanced("(]")'
    expect: 'False'
  - call: 'IsBalanced("")'
    expect: 'True'
  - call: 'IsBalanced("((")'
    expect: 'False'
---
public static bool IsBalanced(string s)
{
    var pairs = new Dictionary<char, char> { [')'] = '(', [']'] = '[', ['}'] = '{' };
    var open = new Stack<char>();
    foreach (var c in s)
    {
        if (pairs.ContainsValue(c)) open.Push(c);
        else if (pairs.TryGetValue(c, out var match) && (open.Count == 0 || open.Pop() != match)) return false;
    }
    return open.Count == 0;
}
