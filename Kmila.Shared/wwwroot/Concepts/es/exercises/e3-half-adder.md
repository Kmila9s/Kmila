**Tu objetivo:** cablea dos asignaciones de salida usando bloques de compuertas y las señales de entrada `a` y `b`.

Un **medio-sumador** calcula la suma de dos números de un solo bit:

| `a` | `b` | `sum` | `cout` |
|---|---|---|---|
| 0 | 0 | 0 | 0 |
| 0 | 1 | 1 | 0 |
| 1 | 0 | 1 | 0 |
| 1 | 1 | 0 | 1 |

La matemática dice: `sum = a XOR b`, `cout = a AND b`. Arrastra los bloques para que coincidan.

**Pista — prueba este árbol exacto:**

- Un bloque de asignación con destino **sum**, alimentado por un bloque **XOR** con `a` a la izquierda y `b` a la derecha.
- Engancha un segundo bloque de asignación abajo con destino **cout**, alimentado por un bloque **AND** con `a` a la izquierda y `b` a la derecha.

> Ambas asignaciones viven fuera de un proceso, así que se disparan concurrentemente — el reproductor resaltará ambas líneas en el mismo ciclo delta.
