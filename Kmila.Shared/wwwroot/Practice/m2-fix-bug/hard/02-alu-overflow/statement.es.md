## ALU — corrige la bandera de desbordamiento con signo

La ALU de 8 bits suma `a + b` en complemento a dos y activa `ovf`
cuando el resultado con signo no cabe en 8 bits. La implementación
actual está "casi bien" — tiene la misma forma que la fórmula del
libro pero está leyendo el bit equivocado.

### Especificación

* `a` y `b` son enteros de 8 bits con signo (complemento a dos).
* Sea `sum = a + b` (truncado a 8 bits).
* Hay desbordamiento con signo **si y solo si** ambos operandos tienen
  el mismo signo y el resultado tiene el signo opuesto.
* Formalmente: `ovf = (a[7] = b[7]) AND (sum[7] /= a[7])`.

### Pista

El bit 7 es el bit de signo en complemento a dos de 8 bits. Leer
cualquier otro bit deja la fórmula sin sentido matemático.
