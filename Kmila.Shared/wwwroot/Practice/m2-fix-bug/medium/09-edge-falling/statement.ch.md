## Edge detector, wrong edge

⚠ 翻译未完成 — 描述以英文显示。

A rising-edge detector for `d`. Sample `d` into `d_prev` on `rising_edge(clk)`. `pulse = '1'` for one cycle when `d_prev = '0'` and `d = '1'`.

The starter detects the falling edge instead.

**Hint:** swap the two equality tests in the conditional assignment for `pulse`.
