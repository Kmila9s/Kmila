## Manchester encoder — half-bit swap

The encoder should follow IEEE 802.3 (G.E. Thomas convention):
- `din = '0'` → `m = '01'` (low half then high half)
- `din = '1'` → `m = '10'` (high half then low half)

That collapses to `m = din xor phase`.

The starter inverts the encoding (returns `'10'` for `0` and `'01'` for `1`). Find and fix the offending operator in the architecture.
