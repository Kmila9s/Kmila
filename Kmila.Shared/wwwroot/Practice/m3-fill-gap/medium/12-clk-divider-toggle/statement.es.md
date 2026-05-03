## Divisor de reloj — rellena el toggle

Un divisor de reloj de 4 bits está **casi** completo: el contador
módulo 10 (`TOP = 9`), el reset y la salida `clk_out <= clk_reg` ya
están listos. Cuando el contador llega a `TOP`, vuelve a `0`.

Debes implementar qué pasa con `clk_reg` **en el mismo instante** del
overflow del contador: debe alternar. Fuera de ese evento, `clk_reg`
conserva su valor.

### Especificación

* Reset asíncrono: `count ← 0`, `clk_reg ← '0'`.
* En `rising_edge(clk)`:
  * si `count = TOP` → `count ← 0`, `clk_reg ← not clk_reg`
  * si no → `count ← count + 1`

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. El contador, el reset
y la salida están bloqueados.

### Pista

`clk_reg <= not clk_reg;`
