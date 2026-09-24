## SPI falling-edge detector — fill the gap

⚠ 翻訳待ち — 説明は英語で表示されます。

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
