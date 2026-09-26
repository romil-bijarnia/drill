---
id: py-008
title: Stack class
lang: py
tags: [data-structures, classes]
modes: [trace, recall, blank]
spec: Define Stack with push, pop, peek and __len__; pop and peek raise IndexError when empty.
tests:
  - call: '(lambda s: (s.push(1), s.push(2), str(s.pop()) + "," + str(s.peek()) + "," + str(len(s)))[-1])(Stack())'
    expect: '2,1,1'
  - call: 'raises(lambda: Stack().pop())'
    expect: 'IndexError'
---
class Stack:
    def __init__(self):
        self._items = []

    def push(self, item):
        self._items.append(item)

    def pop(self):
        if not self._items:
            raise IndexError("stack is empty")
        return self._items.pop()

    def peek(self):
        if not self._items:
            raise IndexError("stack is empty")
        return self._items[-1]

    def __len__(self):
        return len(self._items)
