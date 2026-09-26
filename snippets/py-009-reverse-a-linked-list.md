---
id: py-009
title: Reverse a linked list
lang: py
tags: [data-structures]
modes: [trace, recall, blank]
spec: Define Node(value, next), from_list(values) building a list, to_str(head) joining values with "->", and reverse(head).
tests:
  - call: 'to_str(reverse(from_list([1, 2, 3])))'
    expect: '3->2->1'
  - call: 'to_str(reverse(from_list([])))'
    expect: ''
  - call: 'to_str(from_list([1]))'
    expect: '1'
---
class Node:
    def __init__(self, value, next=None):
        self.value = value
        self.next = next


def from_list(values):
    head = None
    for value in reversed(values):
        head = Node(value, head)
    return head


def to_str(head):
    parts = []
    while head:
        parts.append(str(head.value))
        head = head.next
    return "->".join(parts)


def reverse(head):
    previous = None
    while head:
        head.next, previous, head = previous, head, head.next
    return previous
