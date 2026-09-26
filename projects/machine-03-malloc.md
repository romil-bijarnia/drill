---
id: machine-03-malloc
title: Machine 3 — your own malloc
status: later
stack: [c]
---
A working memory allocator in C: my_malloc, my_free, my_realloc, my_calloc, with block
headers, free-list reuse, splitting and coalescing, backed by mmap. Done means a stress
test of a hundred thousand random allocations and frees passes with no overlapping live
blocks, and the README has the block layout drawn in ASCII.

## Steps
- [ ] A bump allocator over a static 1 MiB array: my_malloc returns 16-byte-aligned blocks, my_free does nothing, and a test program allocates and writes into a hundred blocks
- [ ] A header before every block (size and a free flag); my_free marks the block free; a test walks the heap and prints every block
- [ ] First-fit reuse of free blocks; a test that frees and reallocates without the heap growing
- [ ] Split a free block when it is much larger than the request
- [ ] Coalesce with the next free block on free, then with the previous one (add a footer or a back pointer)
- [ ] my_realloc and my_calloc on top of the above, with a test each
- [ ] Replace the static array with memory from mmap, growing in chunks when needed
- [ ] Stress test: a hundred thousand random allocs and frees, checking after each that no two live blocks overlap and every block is 16-byte aligned; README with the layout drawing; push
