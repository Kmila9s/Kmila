## Loadable register, wrong clock edge

⚠ 翻訳待ち — 説明は英語で表示されます。

A 4-bit register with synchronous load. Async high-active reset. On `rising_edge(clk)`, if `load = '1'` then `q <- d`.

The starter clocks on the falling edge instead.

**Hint:** swap `falling_edge(clk)` for `rising_edge(clk)`.
