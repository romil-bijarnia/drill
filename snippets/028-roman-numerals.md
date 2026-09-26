---
id: 028
title: Roman numerals
tags: [algorithms, strings]
modes: [trace, recall, blank]
spec: Convert a positive integer below 4000 to Roman numerals.
tests:
  - call: 'ToRoman(1994)'
    expect: 'MCMXCIV'
  - call: 'ToRoman(4)'
    expect: 'IV'
  - call: 'ToRoman(58)'
    expect: 'LVIII'
---
public static string ToRoman(int n)
{
    var table = new (int Value, string Symbol)[]
    {
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
        (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
    };
    var result = new StringBuilder();
    foreach (var (value, symbol) in table)
        while (n >= value) { result.Append(symbol); n -= value; }
    return result.ToString();
}
