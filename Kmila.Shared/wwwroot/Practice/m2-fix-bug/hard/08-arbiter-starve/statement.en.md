## 4-way arbiter — fix the starvation

Four agents drive `req(3..0)`. The arbiter must give a single
grant per cycle, but the current implementation is a plain
fixed-priority encoder: the lowest-numbered active request always
wins. When `req(0)` is stuck high, `req(3)` is **starved forever**.

### Specification

* Synchronous, active-high reset.
* Exactly one bit of `grant` is `'1'` per cycle (or `0000` if
  `req = 0000`).
* When several requests are simultaneously high, the arbiter must
  rotate — every requester gets a turn within at most 4 cycles.

### Hint

A round-robin arbiter remembers the **last granted index** and
scans starting from `last_grant + 1` (mod 4). The first active
request encountered in that scan wins.
