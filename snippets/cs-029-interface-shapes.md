---
id: cs-029
title: Interface and records
tags: [oop, records]
modes: [trace, recall, blank]
spec: Define IShape with Area(), records Circle(R) and Rect(W, H) that implement it, and TotalArea over a sequence of shapes.
tests:
  - call: 'TotalArea(new IShape[] { new Rect(2, 3), new Rect(1, 1) })'
    expect: '7'
  - call: 'Math.Round(new Circle(1).Area(), 2)'
    expect: '3.14'
  - call: 'TotalArea(new IShape[0])'
    expect: '0'
---
public interface IShape
{
    double Area();
}

public record Circle(double R) : IShape
{
    public double Area() => Math.PI * R * R;
}

public record Rect(double W, double H) : IShape
{
    public double Area() => W * H;
}

public static double TotalArea(IEnumerable<IShape> shapes) => shapes.Sum(s => s.Area());
