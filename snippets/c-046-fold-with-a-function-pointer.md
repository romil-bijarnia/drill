---
id: c-046
title: Fold with a function pointer
lang: c
tags: [functions, arrays]
modes: [trace, recall, blank]
spec: double fold(const double *xs, size_t n, double init, double (*f)(double, double)) folds from the left, acc = f(acc, xs[i]) starting at init, and returns acc; init when n is 0.
tests:
  - call: 'fold((double[]){1.5, 7, 3}, 3, 0, fmax)'
    expect: '7'
  - call: 'fold((double[]){1.5, 7, 3}, 3, 100, fmin)'
    expect: '1.5'
  - call: 'fold((double[]){3, 4}, 2, 0, hypot)'
    expect: '5'
  - call: 'fold(NULL, 0, 42, fmax)'
    expect: '42'
---
double fold(const double *xs, size_t n, double init, double (*f)(double, double))
{
    double acc = init;
    for (size_t i = 0; i < n; i++) acc = f(acc, xs[i]);
    return acc;
}
