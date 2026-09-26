---
id: cs-034
title: Transpose a matrix
tags: [algorithms, arrays]
modes: [trace, recall, blank]
spec: Transpose a rectangular jagged matrix so rows become columns.
tests:
  - call: 'string.Join("|", Transpose(new[] { new[] {1, 2, 3}, new[] {4, 5, 6} }).Select(r => string.Join(",", r)))'
    expect: '1,4|2,5|3,6'
  - call: 'Transpose(new int[0][]).Length'
    expect: '0'
---
public static int[][] Transpose(int[][] m)
{
    if (m.Length == 0) return m;
    var result = new int[m[0].Length][];
    for (int c = 0; c < m[0].Length; c++)
    {
        result[c] = new int[m.Length];
        for (int r = 0; r < m.Length; r++) result[c][r] = m[r][c];
    }
    return result;
}
