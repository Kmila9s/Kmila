## Sumador con anchos mixtos — bug de extensión de signo

`a` es un número en complemento a dos de 4 bits (rango −8…+7) y `b` es de 8 bits, también en complemento a dos. La salida `sum = a + b` es de 8 bits.

### Especificación

* Extiende `a` de 4 a 8 bits con extensión de **signo**, y luego suma a `b`.
* Replica el bit de signo `a(3)` en los cuatro bits altos del valor extendido.

### Qué falla

El autor extendió `a` añadiendo `"0000"`, lo que es **extensión con ceros**. Funciona por casualidad cuando `a` es positivo, pero con `a` negativo (p.ej. `1111` = −1) genera el valor positivo 15 y la suma queda mal.

### Pista

`numeric_std` ofrece `resize(valor_signed, nuevo_ancho)`, que realiza la extensión de signo automáticamente cuando la fuente es `signed`.
