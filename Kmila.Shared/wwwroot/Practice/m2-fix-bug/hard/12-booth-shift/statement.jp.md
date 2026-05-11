## Booth 乗算器 — シフト誤り

⚠ 翻訳待ち — 説明は英語で表示されます。

A single Booth iteration must perform an arithmetic right-shift on the accumulator. The starter uses a logical right-shift (zero-fill), corrupting negative values. Replace the `unsigned`-based shift with `shift_right(signed(...), 1)`.
