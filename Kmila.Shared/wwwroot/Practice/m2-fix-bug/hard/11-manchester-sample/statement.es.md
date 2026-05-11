## Codificador Manchester — medios-bits cambiados

El codificador debe seguir la convención G.E. Thomas (IEEE 802.3):
- `din = '0'` → `m = '01'`
- `din = '1'` → `m = '10'`

Esto se resume en `m = din xor phase`.

El starter invierte la codificación (devuelve `'10'` para `0` y `'01'` para `1`). Encuentra y corrige el operador defectuoso.
