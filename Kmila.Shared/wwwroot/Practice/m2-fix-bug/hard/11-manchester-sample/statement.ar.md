## مشفِّر مانشستر — تبديل نصفَي البت

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

The encoder should follow IEEE 802.3 (G.E. Thomas convention):
- `din = '0'` → `m = '01'`
- `din = '1'` → `m = '10'`

That collapses to `m = din xor phase`. The starter inverts the encoding. Find and fix the offending operator.
