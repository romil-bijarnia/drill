---
id: ladder-03-data-structures
title: Ladder 3 — data structures from scratch
status: later
stack: [csharp]
---
Rung three: build the containers you normally import, each generic, each with tests,
none using List<T> or Dictionary<TKey,TValue> underneath.

## Steps
- [ ] Stack<T> on a growable array: Push, Pop, Peek, Count; doubles capacity when full
- [ ] Queue<T> as a circular buffer: Enqueue, Dequeue, Peek, Count
- [ ] Singly linked list: AddFirst, AddLast, Remove, Find, enumeration with yield
- [ ] Hash map with separate chaining: Put, Get, Remove, resize at load factor 0.75
- [ ] Binary search tree: Insert, Contains, Remove, in-order traversal
- [ ] Tests for every operation including the empty and single-element cases
- [ ] Push to GitHub with a short write-up of one bug you hit and how you found it
