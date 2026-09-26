---
id: c-054
title: Tower of Hanoi moves
lang: c
tags: [recursion, pointers]
modes: [trace, recall, blank]
spec: void hanoi(int n, char from, char to, char via, long *moves) solves Tower of Hanoi recursively (n - 1 discs to via, one disc to to, n - 1 discs from via to to), adding 1 to *moves per disc moved, so n discs add 2^n - 1.
tests:
  - call: '({ long m = 0; hanoi(3, 'A', 'C', 'B', &m); m; })'
    expect: '7'
  - call: '({ long m = 0; hanoi(1, 'A', 'C', 'B', &m); m; })'
    expect: '1'
  - call: '({ long m = 0; hanoi(0, 'A', 'C', 'B', &m); m; })'
    expect: '0'
  - call: '({ long m = 5; hanoi(10, 'A', 'C', 'B', &m); m; })'
    expect: '1028'
---
void hanoi(int n, char from, char to, char via, long *moves)
{
    if (n == 0) return;
    hanoi(n - 1, from, via, to, moves);
    (*moves)++;
    hanoi(n - 1, via, to, from, moves);
}
