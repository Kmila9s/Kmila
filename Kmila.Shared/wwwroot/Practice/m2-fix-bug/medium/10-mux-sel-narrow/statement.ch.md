## 4-to-1 mux, selector under-decoded

⚠ 翻译未完成 — 描述以英文显示。

A 4-to-1 mux with 2-bit `sel`: `00→a`, `01→b`, `10→c`, `11→d`.

The starter only checks `sel(0)`, collapsing the design to a 2-to-1 mux. Inputs `c` and `d` are unreachable.

**Hint:** the `case` should switch on the whole `sel` vector with branches `"00"`, `"01"`, `"10"`, `"11"`.
