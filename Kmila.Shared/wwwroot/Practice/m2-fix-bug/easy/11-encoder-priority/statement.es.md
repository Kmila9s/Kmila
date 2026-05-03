## Codificador de prioridad 4 a 2 — encuentra y corrige el error

Un codificador de prioridad 4 a 2 entrega el **índice del bit
más alto en `1`** dentro de `d`. El autor escribió una cadena de
cláusulas `when` que recorre los bits en el **orden equivocado**,
así que cuando hay varios bits a `1` devuelve el de menor índice
en vez del más alto.

Encuentra el error y corrige el orden de prioridad.

### Especificación

* Si `d(3) = 1` → `y = "11"`
* Si no, si `d(2) = 1` → `y = "10"`
* Si no, si `d(1) = 1` → `y = "01"`
* Si no → `y = "00"`

### Pista

`when ... else` se evalúa de **arriba hacia abajo** — gana la
primera cláusula que coincide.
