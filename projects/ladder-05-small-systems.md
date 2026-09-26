---
id: ladder-05-small-systems
title: Ladder 5 — small systems
status: active
stack: [csharp]
---
Rung five: three things that look like real infrastructure in miniature. A key-value
store that survives a restart, a rate limiter, and an HTTP server that serves JSON.

## Steps
- [ ] Key-value store: Set/Get/Delete over an append-only log file; restart replays the log
- [ ] Compaction: rewrite the log without dead entries when it doubles in size
- [ ] Token-bucket rate limiter: Allow(clientId) with configurable rate and burst; tests with a fake clock
- [ ] HTTP server with HttpListener: GET /kv/{key} and PUT /kv/{key} backed by the store, JSON in and out
- [ ] Rate-limit the HTTP endpoints per client IP and return 429 with a Retry-After header
- [ ] Load test with 1000 requests from a small client and record the numbers
- [ ] Push to GitHub with a README that explains the log format
