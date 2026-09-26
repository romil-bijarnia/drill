---
id: 044
title: Record with-expression
tags: [records]
modes: [trace, recall, blank]
spec: Define record Person(Name, Age) and Older(Person) returning a copy that is one year older.
tests:
  - call: 'Older(new Person("Ana", 20)).Age'
    expect: '21'
  - call: 'Older(new Person("Ana", 20)).Name'
    expect: 'Ana'
  - call: 'new Person("A", 1) == new Person("A", 1)'
    expect: 'True'
---
public record Person(string Name, int Age);

public static Person Older(Person p) => p with { Age = p.Age + 1 };
