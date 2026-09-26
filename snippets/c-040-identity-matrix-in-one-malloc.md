---
id: c-040
title: Identity matrix in one malloc
lang: c
tags: [memory, arrays]
modes: [trace, recall, blank]
spec: int *identity(size_t n) returns an n x n matrix from a single malloc, row-major so element (r, c) is m[r * n + c], 1 on the diagonal and 0 elsewhere; the caller frees it.
tests:
  - call: '({ int *m = identity(3); int v = m[0] + m[4] + m[8]; free(m); v; })'
    expect: '3'
  - call: '({ int *m = identity(3); int v = m[1] + m[3] + m[5] + m[7]; free(m); v; })'
    expect: '0'
  - call: '({ int *m = identity(4); int t = 0; for (int i = 0; i < 16; i++) t += m[i]; free(m); t; })'
    expect: '4'
  - call: '({ int *m = identity(1); int v = m[0]; free(m); v; })'
    expect: '1'
---
int *identity(size_t n)
{
    int *m = malloc(n * n * sizeof *m);
    if (!m) return NULL;
    for (size_t r = 0; r < n; r++)
        for (size_t c = 0; c < n; c++)
            m[r * n + c] = r == c;
    return m;
}
