## Sumador completo — encuentra y corrige el error

Se pidió al autor implementar un sumador completo de 1 bit. La
salida `sum` es correcta, pero la salida `cout` está **mal**
para al menos una combinación de entradas.

Encuentra el error en la arquitectura y corrígelo.

### Especificación

Para entradas `a`, `b`, `cin` ∈ {0, 1}:

* `sum  = a ⊕ b ⊕ cin`
* `cout = (a ∧ b) ∨ (cin ∧ (a ⊕ b))`

### Pista

El acarreo de salida debe considerar `cin` — no sólo `a` y `b`.
