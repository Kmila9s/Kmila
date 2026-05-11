## 曼彻斯特编码器 — 半位反转

⚠ 翻译未完成 — 描述以英文显示。

The encoder should follow IEEE 802.3 (G.E. Thomas convention):
- `din = '0'` → `m = '01'`
- `din = '1'` → `m = '10'`

That collapses to `m = din xor phase`. The starter inverts the encoding. Find and fix the offending operator.
