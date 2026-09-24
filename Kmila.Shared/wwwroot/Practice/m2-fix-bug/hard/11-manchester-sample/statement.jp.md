## マンチェスタ符号化器 — 半ビット入替

⚠ 翻訳待ち — 説明は英語で表示されます。

The encoder should follow IEEE 802.3 (G.E. Thomas convention):
- `din = '0'` → `m = '01'`
- `din = '1'` → `m = '10'`

That collapses to `m = din xor phase`. The starter inverts the encoding. Find and fix the offending operator.
