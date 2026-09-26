---
id: c-044
title: Reverse a linked list in place
lang: c
tags: [lists, pointers]
modes: [trace, recall, blank]
spec: typedef struct Node { int value; struct Node *next; } Node; Node *list_reverse(Node *head) relinks the nodes in reverse order without allocating and returns the new head; NULL stays NULL.
tests:
  - call: 'list_reverse(&(Node){1, &(Node){2, &(Node){3, NULL}}})->value'
    expect: '3'
  - call: 'list_reverse(&(Node){1, &(Node){2, &(Node){3, NULL}}})->next->value'
    expect: '2'
  - call: 'list_reverse(&(Node){1, &(Node){2, NULL}})->next->next'
    expect: '(null)'
  - call: 'list_reverse(&(Node){1, NULL})->value'
    expect: '1'
  - call: 'list_reverse(NULL)'
    expect: '(null)'
---
typedef struct Node {
    int value;
    struct Node *next;
} Node;

Node *list_reverse(Node *head)
{
    Node *prev = NULL;
    while (head) {
        Node *next = head->next;
        head->next = prev;
        prev = head;
        head = next;
    }
    return prev;
}
