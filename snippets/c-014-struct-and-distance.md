---
id: c-014
title: Struct and distance
lang: c
tags: [structs]
modes: [trace, recall, blank]
spec: typedef struct { double x, y; } Point; double distance(Point a, Point b).
tests:
  - call: 'distance((Point){0, 0}, (Point){3, 4})'
    expect: '5'
  - call: 'distance((Point){1, 1}, (Point){1, 1})'
    expect: '0'
---
typedef struct {
    double x, y;
} Point;

double distance(Point a, Point b)
{
    double dx = a.x - b.x, dy = a.y - b.y;
    return sqrt(dx * dx + dy * dy);
}
