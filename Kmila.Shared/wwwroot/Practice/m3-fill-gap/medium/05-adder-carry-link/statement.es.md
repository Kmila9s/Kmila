## Sumador ripple de 2 bits — rellena la cadena de acarreo

Dos sumadores completos están parcialmente escritos. Los bits de suma
y el acarreo final ya están listos; solo falta el **acarreo interno**
`c1` entre el bit-0 y el bit-1.

Debes asignar `c1` con el acarreo de salida del sumador completo del
bit-0:

```
c1 = (a0·b0) + cin·(a0 ⊕ b0)
```

### Especificación

* Entradas: `a`, `b` (2 bits cada una) y `cin`.
* Salidas: `sum` (2 bits) y `cout`.
* La suma del bit-0 y la suma/`cout` del bit-1 se calculan fuera del
  hueco — el ejercicio solo arregla el cableado de `c1`.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. Las otras
asignaciones concurrentes son andamiaje fijo.

### Pista

`c1 <= (a(0) and b(0)) or (cin and (a(0) xor b(0)));`
