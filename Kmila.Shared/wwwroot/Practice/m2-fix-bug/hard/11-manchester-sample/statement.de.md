## Manchester-Codierer — Halbbits vertauscht

Der Codierer soll der G.E.-Thomas-Konvention (IEEE 802.3) folgen:
- `din = '0'` → `m = '01'`
- `din = '1'` → `m = '10'`

Kompakt: `m = din xor phase`.

Der Starter invertiert die Codierung (`'10'` für `0`, `'01'` für `1`). Finde und korrigiere den fehlerhaften Operator.
