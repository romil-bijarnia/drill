---
id: c-041
title: Linked list length
lang: c
tags: [lists, pointers]
modes: [trace, recall, blank]
spec: typedef struct Node { int value; struct Node *next; } Node; size_t list_len(const Node *head) counts the nodes; 0 for NULL.
tests:
  - call: 'list_len(&(Node){1, &(Node){2, &(Node){3, NULL}}})'
    expect: '3'
  - call: 'list_len(&(Node){7, NULL})'
    expect: '1'
  - call: 'list_len(NULL)'
    expect: '0'
---
typedef struct Node {
    int value;
    struct Node *next;
} Node;

size_t list_len(const Node *head)
{
    size_t n = 0;
    for (; head; head = head->next) n++;
    return n;
}
