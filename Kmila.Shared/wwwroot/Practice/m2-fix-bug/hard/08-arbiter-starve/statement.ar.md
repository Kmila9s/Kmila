## Round-robin arbiter starvation fix

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

Four agents drive `req(3..0)`. The arbiter must give one grant per
cycle, but the current implementation is a fixed-priority encoder:
the lowest-numbered active request always wins, so `req(3)` is
starved while `req(0)` stays high.

### Specification

* Synchronous, active-high reset.
* Exactly one bit of `grant` is `'1'` per cycle (or `0000` when
  `req = 0000`).
* When several requests are simultaneously high, the arbiter must
  rotate — every requester is served within at most 4 cycles.

### Hint

A round-robin arbiter remembers the **last granted index** and
scans starting from `last_grant + 1` (mod 4). The first active
request encountered wins.
