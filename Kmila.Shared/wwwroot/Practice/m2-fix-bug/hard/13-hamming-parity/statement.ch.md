## 汉明(7,4) — 校验位互换

⚠ 翻译未完成 — 描述以英文显示。

The Hamming(7,4) encoder produces a 7-bit code with parity at positions 7, 6, 4 (from MSB) and data at 5, 3, 2, 1.

Expected order (MSB-first): `p1 & p2 & d(0) & p3 & d(1) & d(2) & d(3)`.

The starter swaps `p1` and `p3` in the final concatenation. Find and fix.
