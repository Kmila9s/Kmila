## Contador síncrono de 4 bits — completa el hueco

Un contador de 4 bits está **casi** terminado. El reset, la
detección del flanco de reloj y la asignación de salida ya están
en su lugar.

Implementa la **única línea** que avanza el contador en cada flanco
ascendente.

### Especificación

* Reset asíncrono: `q ← 0000`
* En `rising_edge(clk)` (sin estar en reset): `q ← q + 1`
* Da la vuelta de `1111` a `0000`

### Dónde escribir

Sólo la línea entre los comentarios `-- BEGIN_EDIT` y `-- END_EDIT`
debe modificarse. El resto es la estructura de la entidad que el
evaluador espera.
