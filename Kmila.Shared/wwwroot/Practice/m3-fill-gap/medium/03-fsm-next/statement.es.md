## Próximo estado de FSM de 3 estados

Una FSM tipo Moore con 3 estados está parcialmente escrita. El registro
de estado, el reset y las transiciones desde `"00"` y `"10"` ya están
implementados.

Debes completar la **asignación de próximo estado para el estado `"01"`**:

* si `x = '1'` → `nxt ← "10"`
* si `x = '0'` → `nxt ← "00"`

### Especificación

* Estados: `"00"`, `"01"`, `"10"` (codificados directamente).
* El reset asíncrono coloca la FSM en `"00"`.
* `state` muestra el estado actual.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. Las otras ramas del
`case` y el registro de estado están bloqueados.

### Pista

Un `if x = '1' then … else … end if;` corto basta.
