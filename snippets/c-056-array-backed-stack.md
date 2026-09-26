---
id: c-056
title: Array-backed stack
lang: c
tags: [structs, arrays]
modes: [trace, recall, blank]
spec: typedef struct { int items[8]; int top; } Stack; bool push(Stack *s, int v) stores v and is false when the 8 slots are full; int pop(Stack *s) removes and returns the newest value, -1 when empty. A Stack zeroed with {0} is empty.
tests:
  - call: '({ Stack s = {0}; push(&s, 1); push(&s, 2); int a = pop(&s); a * 10 + pop(&s); })'
    expect: '21'
  - call: 'pop(&(Stack){0})'
    expect: '-1'
  - call: '({ Stack s = {0}; push(&s, 5); pop(&s); pop(&s); })'
    expect: '-1'
  - call: '({ Stack s = {0}; int ok = 1; for (int i = 0; i < 8; i++) ok &= push(&s, i); ok && !push(&s, 9); })'
    expect: '1'
  - call: '({ Stack s = {0}; push(&s, 4); push(&s, 4); pop(&s); s.top; })'
    expect: '1'
---
typedef struct {
    int items[8];
    int top;
} Stack;

bool push(Stack *s, int v)
{
    if (s->top == 8) return false;
    s->items[s->top++] = v;
    return true;
}

int pop(Stack *s)
{
    return s->top == 0 ? -1 : s->items[--s->top];
}
