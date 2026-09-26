---
id: ladder-02-file-tools
title: Ladder 2 — file tools
status: later
stack: [csharp]
---
Rung two: three small command-line tools that read real files. Word count, a CSV
summary, and a log filter with flags. Each one is a separate command in one console app.

## Steps
- [ ] wc <file>: print lines, words and characters; missing file prints an error and exits 1
- [ ] wc handles a 100 MB file without loading it all into memory (stream it)
- [ ] csv <file>: column names, row count, and min/max/average for every numeric column
- [ ] csv copes with quoted fields containing commas
- [ ] logs <file> --level WARN --since 2026-09-01 --grep timeout: filter lines by flags, any combination
- [ ] Tests with small fixture files for all three tools
- [ ] README and push to GitHub
