---
id: py-019
title: Dataclass shapes
lang: py
tags: [classes, dataclasses]
modes: [trace, recall, blank]
spec: Define dataclasses Circle(r) and Rect(w, h) with area() methods, and total_area(shapes). Import dataclass and math yourself.
tests:
  - call: 'total_area([Rect(2, 3), Rect(1, 1)])'
    expect: '7'
  - call: 'round(Circle(1).area(), 2)'
    expect: '3.14'
  - call: 'total_area([])'
    expect: '0'
---
import math
from dataclasses import dataclass


@dataclass
class Circle:
    r: float

    def area(self):
        return math.pi * self.r ** 2


@dataclass
class Rect:
    w: float
    h: float

    def area(self):
        return self.w * self.h


def total_area(shapes):
    return sum(s.area() for s in shapes)
