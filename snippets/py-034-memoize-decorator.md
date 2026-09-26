---
id: py-034
title: Memoize decorator
lang: py
tags: [decorators]
modes: [trace, recall, blank]
spec: A memoize decorator that caches by positional args and counts cache misses in wrapper.misses.
tests:
  - call: '(lambda f: (f(3), f(3), f.misses)[-1])(memoize(lambda n: n * n))'
    expect: '1'
  - call: 'memoize(lambda n: n + 1)(4)'
    expect: '5'
---
def memoize(fn):
    cache = {}

    def wrapper(*args):
        if args not in cache:
            wrapper.misses += 1
            cache[args] = fn(*args)
        return cache[args]

    wrapper.misses = 0
    return wrapper
