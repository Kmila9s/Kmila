## 4 ビット符号なし乗算器

⚠ 翻訳待ち — 説明は英語で表示されます。

## 4-bit unsigned multiplier

Combinational multiplier: `product = a * b` where `a` and `b` are 4-bit unsigned numbers and `product` is 8-bit.

### Spec

- Pure combinational — no clock.
- Use `IEEE.NUMERIC_STD`. The `*` operator is overloaded for `unsigned` operands.
- The natural width of `unsigned(3 downto 0) * unsigned(3 downto 0)` is exactly 8 bits.

### Hint

```vhdl
product <= std_logic_vector(unsigned(a) * unsigned(b));
```
