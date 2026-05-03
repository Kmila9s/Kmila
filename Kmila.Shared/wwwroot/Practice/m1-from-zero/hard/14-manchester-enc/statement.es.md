## Codificador Manchester

Codifica una entrada serial `din` de modo que cada periodo de bit contenga una transición (0→1 para `din=0`, 1→0 para `din=1`).

### Especificación

- `phase` interno alterna cada flanco.
- Reset deja `phase` en `'0'`.
- Salida: `m <= din xor phase;`
- Resultado: con `din=0`, `m` da `0,1,0,1,...` → codifica `'01'`. Con `din=1`, `m` da `1,0,1,0,...` → `'10'`.

### Pista

Un flip-flop que alterna y un xor — el esquema más simple de codificación de línea.
