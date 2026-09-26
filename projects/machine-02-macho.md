---
id: machine-02-macho
title: Machine 2 — Mach-O reader
status: later
stack: [c]
---
Read the binaries your compiler produces, by hand. A C tool that opens a 64-bit Mach-O
file, prints its header, walks the load commands, lists every segment and section, and
dumps the code section through your own hexdump. You define the structs yourself from
the documented layout; you do not include mach-o/loader.h. Done means `otool -l` and
your tool agree on /bin/ls.

## Steps
- [ ] Read the first four bytes of a file and print whether it is a 64-bit Mach-O (0xfeedfacf), a fat binary (0xcafebabe), or neither
- [ ] Define struct mach_header_64 yourself from the field list in the Apple headers and print cputype, filetype, ncmds and sizeofcmds
- [ ] Walk the load commands, printing cmd and cmdsize for each, stepping by cmdsize
- [ ] For LC_SEGMENT_64 print the segment name, vmaddr, vmsize, fileoff and filesize, then each section's name, address and size
- [ ] For LC_MAIN print the entry offset; for LC_LOAD_DYLIB print the library path from the string offset
- [ ] Compare with `otool -l` on /bin/ls and on your hexdump binary; fix every mismatch
- [ ] Dump the bytes of the __text section through your hexdump code from machine-01 (share the source file, don't copy it)
- [ ] README with one annotated run on a real binary; push
