## Banderas FIFO vacío/lleno

Implementa el contador de ocupación de un FIFO de profundidad 4. Sólo expones las banderas `full` y `empty` — los datos no se modelan.

### Especificación

- Contador `unsigned` de 3 bits `cnt` en rango 0..4.
- Cada ciclo:
  - si sólo `push='1'` y `cnt < 4`, `cnt <= cnt + 1`.
  - si sólo `pop='1'` y `cnt > 0`, `cnt <= cnt - 1`.
  - en otro caso mantén el valor.
- `empty <= '1'` cuando `cnt = 0`.
- `full <= '1'` cuando `cnt = 4`.

### Pista

Usa `to_unsigned(4, 3)` para comparar y limita en los extremos.
