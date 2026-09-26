---
id: machine-01-hexdump
title: Machine 1 — hexdump
status: active
stack: [c]
---
Rung one of the machine ladder: a working clone of `hexdump -C` written in C from an empty
file. It reads any file or stdin, prints the offset, sixteen bytes in hex and the ASCII
column, and its output is byte-for-byte identical to the real tool on every file you
throw at it. Done means a Makefile that builds with zero warnings, a README, and a push.

## Steps
- [ ] New hd.c with a main that opens argv[1] with fopen, prints the byte count, and exits 1 with a message when the file can't be opened
- [ ] Read the file in 16-byte chunks with fread and print each byte as two hex digits, sixteen per line
- [ ] Add the offset column: eight hex digits at the start of every line, and the final offset on its own line at the end
- [ ] Add the ASCII column between bars: printable bytes as themselves, everything else as a dot, padded so the last line lines up
- [ ] Read from stdin when no file is given, so `cat f | ./hd` matches `./hd f`
- [ ] Diff your output against `hexdump -C` on an empty file, a 17-byte file, a file with every byte value, and your own binary; fix every difference, including the `*` line for repeated rows
- [ ] `-n N` limits the bytes shown and `-s OFF` skips to an offset, parsed by hand, no getopt
- [ ] Makefile with `-Wall -Wextra -std=c11` and zero warnings, README with usage, push to GitHub
