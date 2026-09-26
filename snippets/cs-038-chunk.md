---
id: cs-038
title: Chunk a sequence
tags: [iterators, yield]
modes: [trace, recall, blank]
spec: Split a sequence into consecutive chunks of the given size; the last chunk may be shorter.
tests:
  - call: 'string.Join("|", Chunk(new[] {1, 2, 3, 4, 5}, 2).Select(c => string.Join(",", c)))'
    expect: '1,2|3,4|5'
  - call: 'Chunk(new int[0], 3).Count()'
    expect: '0'
---
public static IEnumerable<List<int>> Chunk(IEnumerable<int> xs, int size)
{
    var current = new List<int>(size);
    foreach (var x in xs)
    {
        current.Add(x);
        if (current.Count == size) { yield return current; current = new List<int>(size); }
    }
    if (current.Count > 0) yield return current;
}
