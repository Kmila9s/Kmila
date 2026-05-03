## ラウンドロビン調停器(4)

⚠ 翻訳待ち — 説明は英語で表示されます。

## Round-robin arbiter (4)

Implement a 4-channel round-robin arbiter. Each clock, grant exactly one request — `grant` is one-hot (or all zero when no `req` bits are set).

### Spec

- Track `last`, a 2-bit index naming who was granted previously (initial 0 after reset).
- On each rising edge, scan `req` starting at `last + 1` (wrap with 2-bit `unsigned`).
- The first set bit found wins: assert that bit in `grant` and update `last`.
- If no `req` bits are set, `grant` becomes `0000` and `last` stays.
