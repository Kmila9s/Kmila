## Flip-flop T — rellena la línea de toggle

Un flip-flop T está **casi** terminado: el reset, la detección del
flanco ascendente y la salida `q <= qi` ya están implementadas.

Debes implementar el comportamiento de toggle dentro del flanco:

* si `t = '1'` → `qi` cambia de valor
* si `t = '0'` → `qi` se mantiene

### Especificación

* Reset asíncrono: `qi ← '0'`.
* En `rising_edge(clk)`: `qi ← qi xor t`.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. El reset y la salida
están bloqueados.

### Pista

`qi <= qi xor t;`
