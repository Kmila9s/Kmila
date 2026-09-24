## Selector de bit de relleno del registro de desplazamiento

Un registro de desplazamiento de 8 bits ya está cableado: el reset lo
limpia y en cada flanco ascendente desplaza e introduce un bit de
relleno `feed` por la derecha.

Debes implementar cómo se elige `feed`:

* `mode = '1'` → `feed` toma el valor de `din`
* `mode = '0'` → `feed` se fuerza a `'0'`

### Especificación

* Reset asíncrono: `sreg ← 0000_0000`
* En `rising_edge(clk)`: `sreg ← sreg(6..0) & feed`
* `q` siempre refleja `sreg`.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. El proceso de
desplazamiento, el reset y la salida están bloqueados.

### Pista

Una asignación concurrente con `when … else` basta.
