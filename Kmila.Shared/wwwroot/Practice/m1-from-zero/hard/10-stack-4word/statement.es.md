## Banderas de pila 4 palabras

Implementa el contador de ocupación de una pila LIFO de 4 palabras. Sólo expones `full` y `empty`.

### Especificación

- Contador `unsigned` de 3 bits `cnt` en 0..4.
- Cada ciclo:
  - si sólo `push='1'` y `cnt < 4`, `cnt <= cnt + 1`.
  - si sólo `pop='1'` y `cnt > 0`, `cnt <= cnt - 1`.
  - en otro caso mantén (push y pop juntos también — limitar).
- `empty <= '1'` cuando `cnt = 0`.
- `full <= '1'` cuando `cnt = 4`.

### Pista

El control es idéntico a una FIFO de profundidad 4 — sólo cambia el orden de datos, que aquí no se modela.
