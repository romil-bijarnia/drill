---
id: 005
title: Positional record
tags: [records]
modes: [trace, recall]
spec: Define a Point record with X and Y and a method returning the distance from origin.
tests:
  - call: 'new Point(3, 4).Distance()'
    expect: '5'
  - call: 'new Point(0, 0).Distance()'
    expect: '0'
---
public record Point(double X, double Y)
{
    public double Distance() => Math.Sqrt(X * X + Y * Y);
}
