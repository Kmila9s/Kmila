## ⚠ 翻訳作業中 — D flip-flop, async reset that isn't async

A D flip-flop with **async high-active reset**.

* On `rising_edge(clk)`: `q <- d`.
* When `reset = '1'`: `q <- '0'` immediately.

If you assert `reset` between clock edges, `q` does not clear right away — the reset has effectively become synchronous.

**Hint:** sensitivity lists drive when a process re-evaluates. An async reset must be listed alongside `clk`.
