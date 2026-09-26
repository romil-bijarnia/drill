---
id: 019
title: Reverse a linked list
tags: [data-structures]
modes: [trace, recall, blank]
spec: Define Node (Value, Next), From(params int[]) that builds a list, Print(Node) joining values with "->", and Reverse(Node).
tests:
  - call: 'Print(Reverse(From(1, 2, 3)))'
    expect: '3->2->1'
  - call: 'Print(Reverse(From()))'
    expect: ''
  - call: 'Print(From(1))'
    expect: '1'
---
public class Node
{
    public int Value;
    public Node Next;
    public Node(int value) => Value = value;
}

public static Node From(params int[] values)
{
    Node head = null;
    for (int i = values.Length - 1; i >= 0; i--) head = new Node(values[i]) { Next = head };
    return head;
}

public static string Print(Node head)
{
    var parts = new List<string>();
    for (var n = head; n != null; n = n.Next) parts.Add(n.Value.ToString());
    return string.Join("->", parts);
}

public static Node Reverse(Node head)
{
    Node previous = null;
    while (head != null)
    {
        var next = head.Next;
        head.Next = previous;
        previous = head;
        head = next;
    }
    return previous;
}
