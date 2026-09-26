---
id: py-001
title: FizzBuzz
lang: py
tags: [basics]
modes: [trace, recall, blank]
spec: Return "Fizz" for multiples of 3, "Buzz" for multiples of 5, "FizzBuzz" for both, otherwise the number as a string.
tests:
  - call: 'fizzbuzz(3)'
    expect: 'Fizz'
  - call: 'fizzbuzz(10)'
    expect: 'Buzz'
  - call: 'fizzbuzz(15)'
    expect: 'FizzBuzz'
  - call: 'fizzbuzz(7)'
    expect: '7'
---
def fizzbuzz(n):
    if n % 15 == 0:
        return "FizzBuzz"
    if n % 3 == 0:
        return "Fizz"
    if n % 5 == 0:
        return "Buzz"
    return str(n)
