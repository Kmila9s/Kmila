## ⚠ 翻訳作業中 — T flip-flop, enable input ignored

A T flip-flop with toggle enable `t`. Async high-active reset clears `q`. On `rising_edge(clk)`, toggle `q` only when `t = '1'`; otherwise hold.

The starter toggles every clock regardless of `t`.

**Hint:** wrap the toggle inside `if t = '1' then ... end if;` in the rising-edge branch.
