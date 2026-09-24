## Contador up/down — rellena la línea de dirección

Un contador síncrono de 4 bits está **casi** terminado. El reset, el
flanco ascendente y la salida `q <= std_logic_vector(count)` ya están
implementados.

Debes implementar el control de dirección en el flanco:

* `dir = '1'` → `count ← count + 1`
* `dir = '0'` → `count ← count - 1`

### Especificación

* Reset asíncrono: `count ← 0000`.
* El contador hace wrap libremente (aritmética unsigned de 4 bits).

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. El reset y la salida
están bloqueados.

### Pista

Un `if dir = '1' then … else … end if;` corto basta.
