---
id: c-062
title: Insertion sort
lang: c
tags: [algorithms, arrays]
family: isort
modes: [trace, recall, blank]
spec: long *isort(long *xs, long n) sorts n values in place with insertion sort and returns xs.
tests:
  - call: 'isort((long[]){3, 1, 2}, 3)[0]'
    expect: '1'
  - call: 'isort((long[]){3, 1, 2}, 3)[1]'
    expect: '2'
  - call: 'isort((long[]){3, 1, 2}, 3)[2]'
    expect: '3'
  - call: 'isort((long[]){9, 7, 8, 1}, 4)[3]'
    expect: '9'
  - call: 'isort((long[]){7}, 1)[0]'
    expect: '7'
---
long *isort(long *xs, long n)
{
    for (long i = 1; i < n; i++) {
        long key = xs[i];
        long j = i - 1;
        while (j >= 0 && xs[j] > key) {
            xs[j + 1] = xs[j];
            j--;
        }
        xs[j + 1] = key;
    }
    return xs;
}
