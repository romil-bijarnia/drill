---
id: cs-011
title: FizzBuzz
tags: [basics, pattern-matching]
modes: [trace, recall, blank]
spec: Return "Fizz" for multiples of 3, "Buzz" for multiples of 5, "FizzBuzz" for both, otherwise the number as text.
tests:
  - call: 'FizzBuzz(3)'
    expect: 'Fizz'
  - call: 'FizzBuzz(10)'
    expect: 'Buzz'
  - call: 'FizzBuzz(15)'
    expect: 'FizzBuzz'
  - call: 'FizzBuzz(7)'
    expect: '7'
---
public static string FizzBuzz(int n) => (n % 3, n % 5) switch
{
    (0, 0) => "FizzBuzz",
    (0, _) => "Fizz",
    (_, 0) => "Buzz",
    _ => n.ToString()
};
