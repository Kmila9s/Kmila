## Mixed-width signed adder, sign extension bug

⚠ 翻译未完成 — 描述以英文显示。

`a` is 4-bit two's-complement (−8…+7), `b` is 8-bit two's-complement. `sum = a + b` is 8 bits.

The author extended `a` by prepending `"0000"`, which is **zero-extension**. For negative `a` (e.g. `1111`), this produces +15 instead of −1, so the sum is wrong.

**Hint:** use `resize(signed(a), 8)` from `numeric_std` for proper sign-extension.
