---
id: 031
title: Rock paper scissors
tags: [pattern-matching]
modes: [trace, recall, blank]
spec: Given two of "rock", "paper", "scissors", return "a" if the first wins, "b" if the second wins, "draw" otherwise.
tests:
  - call: 'Rps("rock", "scissors")'
    expect: 'a'
  - call: 'Rps("paper", "scissors")'
    expect: 'b'
  - call: 'Rps("rock", "rock")'
    expect: 'draw'
---
public static string Rps(string a, string b) => (a, b) switch
{
    _ when a == b => "draw",
    ("rock", "scissors") or ("scissors", "paper") or ("paper", "rock") => "a",
    _ => "b"
};
