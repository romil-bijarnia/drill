---
id: 006
title: Guarded divide
tags: [exceptions]
modes: [trace, recall, blank]
spec: Divide a by b, throwing DivideByZeroException with message "b was zero" when b is 0.
tests:
  - call: 'SafeDivide(10, 2)'
    expect: '5'
  - call: '((Func<string>)(() => { try { SafeDivide(1, 0); return "no throw"; } catch (DivideByZeroException e) { return e.Message; } }))()'
    expect: 'b was zero'
---
public static int SafeDivide(int a, int b)
{
    if (b == 0) throw new DivideByZeroException("b was zero");
    return a / b;
}
