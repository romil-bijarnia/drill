---
id: py-029
title: Bank account class
lang: py
tags: [classes, exceptions]
modes: [trace, recall, blank]
spec: Define Account with a balance property, deposit (ValueError for amounts of zero or less) and withdraw returning False when funds are short.
tests:
  - call: '(lambda a: (a.deposit(100), a.withdraw(30), a.withdraw(500), a.balance)[1:])(Account())'
    expect: '(True, False, 70)'
  - call: 'raises(lambda: Account().deposit(0))'
    expect: 'ValueError'
---
class Account:
    def __init__(self):
        self._balance = 0

    @property
    def balance(self):
        return self._balance

    def deposit(self, amount):
        if amount <= 0:
            raise ValueError("amount must be positive")
        self._balance += amount

    def withdraw(self, amount):
        if amount > self._balance:
            return False
        self._balance -= amount
        return True
