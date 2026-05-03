## ⚠ ترجمة قيد الإنجاز — Loadable register, wrong clock edge

A 4-bit register with synchronous load. Async high-active reset. On `rising_edge(clk)`, if `load = '1'` then `q <- d`.

The starter clocks on the falling edge instead.

**Hint:** swap `falling_edge(clk)` for `rising_edge(clk)`.
