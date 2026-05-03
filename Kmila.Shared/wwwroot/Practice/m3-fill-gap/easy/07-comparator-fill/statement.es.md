## Comparador de igualdad de 4 bits — completa el hueco

Implementa un comparador de igualdad de 4 bits. La salida `eq` vale
`1` cuando los dos vectores `a` y `b` son bit a bit iguales, y `0`
en caso contrario.

### Comportamiento

* `eq <= '1'` cuando `a = b`
* `eq <= '0'` en otro caso

### Pista

Usa una asignación concurrente `when ... else` con el operador `=`
aplicado a los vectores completos.

### Dónde escribir

La entidad, los puertos y la estructura ya están listos. Sólo la
línea entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúa — todo lo
demás es estructura bloqueada.
