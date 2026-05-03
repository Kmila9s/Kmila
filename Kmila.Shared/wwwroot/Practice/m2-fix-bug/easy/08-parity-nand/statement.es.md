## Paridad de 4 bits — encuentra y corrige el error

Este circuito debe entregar la **paridad** del vector de 4 bits
`d`: `p = 1` cuando un número impar de bits de `d` son `1`,
`p = 0` en otro caso. El autor intentó encadenar compuertas NAND
y se equivocó.

Encuentra el error y usa el operador correcto.

### Especificación

* `p = d(0) ⊕ d(1) ⊕ d(2) ⊕ d(3)`

### Pista

La paridad es justo lo que calcula el XOR; un encadenamiento de
NAND no.
