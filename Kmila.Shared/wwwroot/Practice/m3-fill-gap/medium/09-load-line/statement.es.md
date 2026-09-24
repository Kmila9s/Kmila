## Registro de 4 bits con carga — rellena la rama de load

Un registro de 4 bits con carga síncrona está parcialmente escrito.
El reset, el flanco ascendente y la salida `q <= qi` ya están listos.

Debes implementar la **rama de load** dentro del flanco:

* si `load = '1'` → `qi ← d`
* si `load = '0'` → `qi` conserva su valor

### Especificación

* Reset asíncrono: `qi ← 0000`.
* En `rising_edge(clk)`:
  * `load = '1'` captura `d`.
  * `load = '0'` mantiene.
* `q` siempre refleja `qi`.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. El reset y la salida
están bloqueados.

### Pista

Un `if load = '1' then qi <= d; end if;` basta — VHDL mantiene `qi`
cuando no hay asignación.
