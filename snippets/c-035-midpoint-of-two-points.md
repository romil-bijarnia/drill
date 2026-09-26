---
id: c-035
title: Midpoint of two points
lang: c
tags: [structs, basics]
modes: [trace, recall, blank]
spec: typedef struct { double x, y; } Point; Point midpoint(Point a, Point b) returns the point halfway between them by value.
tests:
  - call: 'midpoint((Point){0, 0}, (Point){4, 2}).x'
    expect: '2'
  - call: 'midpoint((Point){0, 0}, (Point){4, 2}).y'
    expect: '1'
  - call: 'midpoint((Point){-3, 1}, (Point){3, 1}).x'
    expect: '0'
  - call: 'midpoint((Point){1, 1}, (Point){2, 1}).x'
    expect: '1.5'
---
typedef struct {
    double x, y;
} Point;

Point midpoint(Point a, Point b)
{
    return (Point){(a.x + b.x) / 2, (a.y + b.y) / 2};
}
