## Contador arriba/abajo — dirección ignorada

Se pedía un contador up/down de 4 bits controlado por `dir`:

* `dir = '1'` → cuenta arriba: `count <- count + 1`.
* `dir = '0'` → cuenta abajo: `count <- count - 1`.
* `reset` asíncrono activo en alto pone `count` a 0.

### Qué falla

El starter solo incrementa — `dir` no se usa. Con `dir = '0'` la cuenta sigue siendo `0 → 1 → 2 → …` en lugar de envolver `0 → 15 → 14 → …`.

### Pista

Necesitas un `if dir = '1' then count <= count + 1; else count <= count - 1; end if;` dentro de la rama del flanco.
