## Contador de 3 bits con wrap — rellena el hueco

Un contador síncrono de 3 bits está **casi** terminado. El reset, la
detección del flanco de reloj y la asignación a `q` ya están cableados.

Implementa el cuerpo de la rama `rising_edge(clk)` para que el contador
recorra `000 → 001 → … → 111 → 000`, es decir, vuelva a cero al llegar
a siete.

### Especificación

* Reset asíncrono: `count ← 000`
* En `rising_edge(clk)`:
  * si `count = 111` → `count ← 000`
  * si no → `count ← count + 1`
* `q` siempre refleja `count`.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. Todo lo demás es
andamiaje fijo que el corrector espera intacto.

### Pista

Compara `count` con `to_unsigned(7, 3)` (o `"111"`) y elige entre
`(others => '0')` y `count + 1`.
