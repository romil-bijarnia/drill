---
id: c-043
title: Linked list find
lang: c
tags: [lists, pointers, search]
modes: [trace, recall, blank]
spec: typedef struct Node { int value; struct Node *next; } Node; Node *list_find(Node *head, int value) returns the first node holding value, or NULL.
tests:
  - call: 'list_find(&(Node){1, &(Node){2, &(Node){3, NULL}}}, 2)->value'
    expect: '2'
  - call: 'list_find(&(Node){1, &(Node){2, &(Node){3, NULL}}}, 2)->next->value'
    expect: '3'
  - call: 'list_find(&(Node){1, NULL}, 9)'
    expect: '(null)'
  - call: 'list_find(NULL, 1)'
    expect: '(null)'
---
typedef struct Node {
    int value;
    struct Node *next;
} Node;

Node *list_find(Node *head, int value)
{
    for (; head; head = head->next)
        if (head->value == value) return head;
    return NULL;
}
