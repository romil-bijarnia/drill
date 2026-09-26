---
id: ladder-01-calculator
title: Ladder 1 — command-line calculator
status: active
stack: [csharp]
---
Rung one of the ladder from the coach prompt: parse an expression, handle errors, loop.
A console app you can type "12 * 3" into and get 36, that never crashes on bad input,
and that you wrote from an empty Program.cs with no AI in the file.

## Steps
- [ ] New console project; read one line from the user and echo it back
- [ ] Parse "a op b" with integer operands and + - * /; print the result
- [ ] Bad input and division by zero print a one-line message and the program keeps running
- [ ] Loop until the user types quit; empty lines are ignored
- [ ] Decimal and negative numbers work (parse as decimal, invariant culture)
- [ ] Ten tests for the parser and evaluator in a test project
- [ ] Stretch: full precedence with parentheses via a recursive-descent parser
- [ ] README with usage, then push to GitHub
