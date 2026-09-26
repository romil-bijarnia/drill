---
id: 035
title: Shortest path with BFS
tags: [algorithms, graphs]
modes: [trace, recall, blank]
spec: Fewest edges from start to goal in a directed graph given as Dictionary<int, int[]>, or -1 when unreachable.
tests:
  - call: 'ShortestPath(new Dictionary<int, int[]> { [1] = new[] {2, 3}, [2] = new[] {4}, [3] = new[] {4}, [4] = new int[0] }, 1, 4)'
    expect: '2'
  - call: 'ShortestPath(new Dictionary<int, int[]> { [1] = new[] {2}, [2] = new int[0] }, 2, 1)'
    expect: '-1'
  - call: 'ShortestPath(new Dictionary<int, int[]> { [1] = new int[0] }, 1, 1)'
    expect: '0'
---
public static int ShortestPath(Dictionary<int, int[]> graph, int start, int goal)
{
    var distance = new Dictionary<int, int> { [start] = 0 };
    var queue = new Queue<int>();
    queue.Enqueue(start);
    while (queue.Count > 0)
    {
        var node = queue.Dequeue();
        if (node == goal) return distance[node];
        foreach (var next in graph.GetValueOrDefault(node) ?? Array.Empty<int>())
        {
            if (distance.ContainsKey(next)) continue;
            distance[next] = distance[node] + 1;
            queue.Enqueue(next);
        }
    }
    return -1;
}
