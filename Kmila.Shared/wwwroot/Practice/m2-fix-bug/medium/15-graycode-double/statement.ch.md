## 3-bit binary-to-Gray, Gray property broken

⚠ 翻译未完成 — 描述以英文显示。

Standard binary-to-Gray: `g(2) = b(2)`, `g(1) = b(2) xor b(1)`, `g(0) = b(1) xor b(0)`.

The starter mis-derives `g(2)` by folding `b(0)` into it. Going from `b=000` to `b=001` flips both `g(0)` and `g(2)` — two bits change, breaking Gray code.

**Hint:** `g(2)` must depend only on the MSB of `b`.
