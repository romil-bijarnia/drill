---
id: py-036
title: Find emails with a regex
lang: py
tags: [regex]
modes: [trace, recall, blank]
spec: Return every email-like token (word chars and dots, @, word chars and dots) in order. Import re yourself.
tests:
  - call: 'find_emails("mail a@b.com or c@d.org")'
    expect: '['a@b.com', 'c@d.org']'
  - call: 'find_emails("none here")'
    expect: '[]'
---
import re


def find_emails(text):
    return re.findall(r"[\w.]+@[\w.]+", text)
