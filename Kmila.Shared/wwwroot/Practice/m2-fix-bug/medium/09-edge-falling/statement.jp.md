## Edge detector, wrong edge

⚠ 翻訳待ち — 説明は英語で表示されます。

A rising-edge detector for `d`. Sample `d` into `d_prev` on `rising_edge(clk)`. `pulse = '1'` for one cycle when `d_prev = '0'` and `d = '1'`.

The starter detects the falling edge instead.

**Hint:** swap the two equality tests in the conditional assignment for `pulse`.
