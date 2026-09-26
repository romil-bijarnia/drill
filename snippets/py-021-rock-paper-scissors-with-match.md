---
id: py-021
title: Rock paper scissors with match
lang: py
tags: [pattern-matching]
modes: [trace, recall, blank]
spec: Given two of "rock", "paper", "scissors", return "a" if the first wins, "b" if the second wins, "draw" otherwise, using match.
tests:
  - call: 'rps("rock", "scissors")'
    expect: 'a'
  - call: 'rps("paper", "scissors")'
    expect: 'b'
  - call: 'rps("rock", "rock")'
    expect: 'draw'
---
def rps(a, b):
    match (a, b):
        case _ if a == b:
            return "draw"
        case ("rock", "scissors") | ("scissors", "paper") | ("paper", "rock"):
            return "a"
        case _:
            return "b"
