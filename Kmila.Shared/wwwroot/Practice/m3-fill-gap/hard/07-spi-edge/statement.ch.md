## SPI falling-edge detector — fill the gap

⚠ 翻译未完成 — 描述以英文显示。

Detect the falling edge of `sclk` in the fast `clk` domain by
comparing the previous-tick history `sclk_prev` against the
current `sclk`.

### Hint

```vhdl
sample <= sclk_prev and (not sclk);
```

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is
graded.
