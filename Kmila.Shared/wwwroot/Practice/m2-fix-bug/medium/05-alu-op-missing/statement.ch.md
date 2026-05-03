## ⚠ 翻译待完成 — 4-bit ALU, missing operation

A 4-operation ALU selected by `op`: `00 = a+b`, `01 = a-b`, `10 = a and b`, `11 = a xor b`.

For `op = "11"` the output is hardwired to `"0000"` instead of `a xor b`. The other three ops are fine.

**Hint:** the `when others` branch of the `case` should compute `a xor b`, not return a constant.
