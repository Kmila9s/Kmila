## 3-state FSM, unreachable state

⚠ 翻译未完成 — 描述以英文显示。

A 3-state machine with states `S0` (`"00"`), `S1` (`"01"`), `S2` (`"10"`).

* Async high-active reset → `S0`.
* `S0 -> S1` always; `S1 -> S2` when `x = '1'`; `S2 -> S0` always.

The `S1 -> S2` transition never fires — `S2` is unreachable. The bug is in the boolean guard.

**Hint:** a signal cannot equal two values at once. The condition is a contradiction.
