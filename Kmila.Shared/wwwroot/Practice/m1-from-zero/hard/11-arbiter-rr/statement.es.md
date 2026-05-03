## Árbitro round-robin (4)

Implementa un árbitro round-robin de 4 canales. Cada reloj, concede exactamente una petición — `grant` es one-hot (o todo cero si no hay peticiones).

### Especificación

- Guarda `last`, índice de 2 bits con la última concesión (0 tras reset).
- En cada flanco, recorre `req` empezando en `last + 1` (con wrap-around de 2 bits).
- El primer bit a `'1'` gana: actívalo en `grant` y actualiza `last`.
- Si `req` es todo cero, `grant` es `0000` y `last` queda igual.

### Pista

Usa `for i in 0 to 3 loop` con una variable de guardia para detenerte al primer match. `to_integer(idx)` indexa el vector.
