---
id: cs-032
title: Average of the top three
tags: [linq]
modes: [trace, recall, blank]
spec: Average of the three largest values, rounded to two decimals; 0 for an empty sequence.
tests:
  - call: 'TopAverage(new[] {1, 5, 3, 9, 7})'
    expect: '7'
  - call: 'TopAverage(new[] {4})'
    expect: '4'
  - call: 'TopAverage(new int[0])'
    expect: '0'
  - call: 'TopAverage(new[] {1, 2})'
    expect: '1.5'
---
public static double TopAverage(IEnumerable<int> xs)
{
    var top = xs.OrderByDescending(x => x).Take(3).ToList();
    return top.Count == 0 ? 0 : Math.Round(top.Average(), 2);
}
