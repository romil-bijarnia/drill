---
id: cs-039
title: Bank account class
tags: [oop]
modes: [trace, recall, blank]
spec: Define Account with a read-only Balance, Deposit (throws ArgumentOutOfRangeException for amounts of zero or less) and Withdraw returning false when funds are short.
tests:
  - call: '((Func<string>)(() => { var a = new Account(); a.Deposit(100); var ok = a.Withdraw(30); var no = a.Withdraw(500); return a.Balance + "," + ok + "," + no; }))()'
    expect: '70,True,False'
  - call: '((Func<string>)(() => { try { new Account().Deposit(0); return "no throw"; } catch (ArgumentOutOfRangeException) { return "threw"; } }))()'
    expect: 'threw'
---
public class Account
{
    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Balance += amount;
    }

    public bool Withdraw(decimal amount)
    {
        if (amount > Balance) return false;
        Balance -= amount;
        return true;
    }
}
