## Comparador "mayor que" de 4 bits — rellena el hueco

Se te da una entidad con dos operandos de 4 bits `a` y `b`, y una
salida `gt`. La arquitectura está vacía salvo la región editable.

Debes asignar `gt`:

* `gt = '1'` cuando `a > b` (sin signo)
* `gt = '0'` en caso contrario

### Especificación

* Trata `a` y `b` como **sin signo**.
* Combinacional puro — no hay reloj.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. La entidad está
bloqueada.

### Pista

`gt <= '1' when unsigned(a) > unsigned(b) else '0';`
