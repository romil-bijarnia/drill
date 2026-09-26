---
id: c-037
title: Best scorer in a struct array
lang: c
tags: [structs, arrays, search]
modes: [trace, recall, blank]
spec: typedef struct { const char *name; int score; } Player; const char *best_name(const Player *ps, int n) returns the name with the highest score among n >= 1 players, the first on ties.
tests:
  - call: 'best_name((Player[]){{"ann", 3}, {"bob", 7}, {"cy", 5}}, 3)'
    expect: 'bob'
  - call: 'best_name((Player[]){{"a", 2}, {"b", 2}}, 2)'
    expect: 'a'
  - call: 'best_name((Player[]){{"solo", -1}}, 1)'
    expect: 'solo'
  - call: 'best_name((Player[]){{"x", 0}, {"y", -9}, {"z", 1}}, 3)'
    expect: 'z'
---
typedef struct {
    const char *name;
    int score;
} Player;

const char *best_name(const Player *ps, int n)
{
    const Player *best = ps;
    for (int i = 1; i < n; i++)
        if (ps[i].score > best->score) best = &ps[i];
    return best->name;
}
