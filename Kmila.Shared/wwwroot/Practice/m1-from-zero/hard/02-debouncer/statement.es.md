## Antirrebote de botón

Filtra una entrada de botón ruidosa. La salida `clean` cambia sólo cuando `raw` se mantuvo estable durante **N = 4** ciclos consecutivos.

### Especificación

- Guarda el último valor muestreado de `raw` y un contador de 2 bits.
- Cada ciclo: si `raw == last`, incrementa el contador; al saturar en `"11"`, copia `raw` a la salida.
- Si `raw` cambia, reinicia el contador y actualiza `last`.
- Reset limpia todo el estado interno y `clean` queda en `'0'`.

### Pista

Usa `unsigned(1 downto 0)` para el contador y un `STD_LOGIC` para el nivel estable.
