---
id: cs-018
title: Stack on a list
tags: [data-structures, generics]
modes: [trace, recall, blank]
spec: Define Stack2<T> with Push, Pop, Peek and Count; Pop and Peek throw InvalidOperationException when empty.
tests:
  - call: '((Func<string>)(() => { var s = new Stack2<int>(); s.Push(1); s.Push(2); return s.Pop() + "," + s.Peek() + "," + s.Count; }))()'
    expect: '2,1,1'
  - call: '((Func<string>)(() => { try { new Stack2<string>().Pop(); return "no throw"; } catch (InvalidOperationException) { return "threw"; } }))()'
    expect: 'threw'
---
public class Stack2<T>
{
    private readonly List<T> _items = new();

    public int Count => _items.Count;

    public void Push(T item) => _items.Add(item);

    public T Pop()
    {
        var item = Peek();
        _items.RemoveAt(_items.Count - 1);
        return item;
    }

    public T Peek() => _items.Count > 0 ? _items[^1] : throw new InvalidOperationException("stack is empty");
}
