---
id: py-025
title: Shortest path with BFS
lang: py
tags: [algorithms, graphs]
modes: [trace, recall, blank]
spec: Fewest edges from start to goal in a directed graph given as a dict of lists, or -1 when unreachable. Import deque yourself.
tests:
  - call: 'shortest_path({1: [2, 3], 2: [4], 3: [4], 4: []}, 1, 4)'
    expect: '2'
  - call: 'shortest_path({1: [2], 2: []}, 2, 1)'
    expect: '-1'
  - call: 'shortest_path({1: []}, 1, 1)'
    expect: '0'
---
from collections import deque


def shortest_path(graph, start, goal):
    distance = {start: 0}
    queue = deque([start])
    while queue:
        node = queue.popleft()
        if node == goal:
            return distance[node]
        for nxt in graph.get(node, []):
            if nxt not in distance:
                distance[nxt] = distance[node] + 1
                queue.append(nxt)
    return -1
