---
id: c-015
title: Linked list push and sum
lang: c
tags: [data-structures, pointers]
modes: [trace, recall, blank]
spec: typedef struct Node { int value; struct Node *next; } Node; Node *push_front(Node *head, int v) allocates; int list_sum(const Node *head).
tests:
  - call: 'list_sum(push_front(push_front(NULL, 1), 2))'
    expect: '3'
  - call: 'list_sum(NULL)'
    expect: '0'
---
typedef struct Node {
    int value;
    struct Node *next;
} Node;

Node *push_front(Node *head, int value)
{
    Node *node = malloc(sizeof *node);
    node->value = value;
    node->next = head;
    return node;
}

int list_sum(const Node *head)
{
    int total = 0;
    for (; head; head = head->next) total += head->value;
    return total;
}
