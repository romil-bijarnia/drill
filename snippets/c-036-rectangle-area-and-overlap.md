---
id: c-036
title: Rectangle area and overlap
lang: c
tags: [structs, basics]
modes: [trace, recall, blank]
spec: typedef struct { int x, y, w, h; } Rect; int rect_area(Rect r); bool rect_overlap(Rect a, Rect b) is true only when they share positive area, so touching edges do not count.
tests:
  - call: 'rect_area((Rect){0, 0, 3, 4})'
    expect: '12'
  - call: 'rect_area((Rect){5, 5, 0, 9})'
    expect: '0'
  - call: 'rect_overlap((Rect){0, 0, 2, 2}, (Rect){1, 1, 2, 2})'
    expect: 'true'
  - call: 'rect_overlap((Rect){0, 0, 2, 2}, (Rect){2, 0, 2, 2})'
    expect: 'false'
  - call: 'rect_overlap((Rect){0, 0, 5, 5}, (Rect){1, 1, 1, 1})'
    expect: 'true'
---
typedef struct {
    int x, y, w, h;
} Rect;

int rect_area(Rect r)
{
    return r.w * r.h;
}

bool rect_overlap(Rect a, Rect b)
{
    return a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;
}
