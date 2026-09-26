---
id: c-042
title: Linked list append
lang: c
tags: [lists, pointers]
modes: [trace, recall, blank]
spec: typedef struct Node { int value; struct Node *next; } Node; Node *list_append(Node *head, Node *node) links node after the last node and returns the head (node itself when head is NULL), walking a Node ** to the final next pointer.
tests:
  - call: 'list_append(&(Node){1, NULL}, &(Node){2, NULL})->next->value'
    expect: '2'
  - call: 'list_append(&(Node){1, NULL}, &(Node){2, NULL})->value'
    expect: '1'
  - call: 'list_append(&(Node){1, &(Node){2, NULL}}, &(Node){3, NULL})->next->next->value'
    expect: '3'
  - call: 'list_append(NULL, &(Node){5, NULL})->value'
    expect: '5'
---
typedef struct Node {
    int value;
    struct Node *next;
} Node;

Node *list_append(Node *head, Node *node)
{
    Node **link = &head;
    while (*link) link = &(*link)->next;
    *link = node;
    return head;
}
