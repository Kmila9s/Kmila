## Booth 乘法器 — 移位错误

⚠ 翻译未完成 — 描述以英文显示。

A single Booth iteration must perform an arithmetic right-shift on the accumulator. The starter uses a logical right-shift (zero-fill), corrupting negative values. Replace the `unsigned`-based shift with `shift_right(signed(...), 1)`.
