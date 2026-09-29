# Ejemplo: tu primera entidad (MUX 2 a 1)

Un **multiplexor** es el dispositivo combinacional más simple que incluye una "decisión". La versión 2 a 1 escoge entre dos entradas según un bit de `sel`:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

En VHDL es una sola línea:

```vhdl
y <= a when sel = '0' else b;
```

## Qué hace el ejemplo

Al abrirlo obtienes un proyecto llamado **Tu primera entidad** con:

- `mux21.vhd` (el diseño de arriba)
- Una configuración de simulación que estimula `sel`, `a` y `b` durante 40 microsegundos

Al pulsar Ejecutar, la pestaña de formas de onda muestra cuatro filas — las tres entradas y la salida. Observa cómo `y` alterna entre `a` y `b` cada vez que `sel` cambia.

## Lo que hay que notar

La asignación `y <= a when sel = '0' else b;` es una **sentencia concurrente**: vive fuera de cualquier proceso y describe un cableado permanente. No hay "¿cuándo se ejecuta esta línea?" — simplemente existe.

La condición dentro de `when/else` puede ser cualquier expresión booleana:

```vhdl
y <= a when sel = '0' else
     b when enable = '1' else
     'Z';
```

Cadenas así se sintetizan como multiplexores con prioridad (gana la condición más arriba).

## Tu turno: extiéndelo

Cuando el ejemplo funcione:

1. Cambia la entidad a un **MUX 4 a 1** — `sel` se vuelve de 2 bits y eliges entre `a`, `b`, `c`, `d`.
2. Agrega un quinto puerto `output_enable`. Cuando valga `'0'`, manda `y <= 'Z'` (alta impedancia) sin importar `sel`.
3. Compara `when/else` contra la versión `with/select` que verás en el siguiente módulo.

El editor de Kmila resalta las keywords de VHDL y conoce `std_logic` — pasa el ratón por `when` para ver la documentación incorporada.
