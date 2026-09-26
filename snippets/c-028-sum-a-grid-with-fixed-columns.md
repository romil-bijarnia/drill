---
id: c-028
title: Sum a grid with fixed columns
lang: c
tags: [arrays, pointers]
modes: [trace, recall, blank]
spec: int sum_grid(int rows, const int grid[][3]) totals a rows x 3 grid passed as a pointer to rows of three; 0 when rows is 0.
tests:
  - call: 'sum_grid(2, (const int[][3]){{1, 2, 3}, {4, 5, 6}})'
    expect: '21'
  - call: 'sum_grid(1, (const int[][3]){{-1, 0, 1}})'
    expect: '0'
  - call: 'sum_grid(0, NULL)'
    expect: '0'
  - call: 'sum_grid(3, (const int[][3]){{1, 1, 1}, {1, 1, 1}, {1, 1, 1}})'
    expect: '9'
---
int sum_grid(int rows, const int grid[][3])
{
    int total = 0;
    for (int r = 0; r < rows; r++)
        for (int c = 0; c < 3; c++)
            total += grid[r][c];
    return total;
}
