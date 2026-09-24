# Juntándolo todo

Los diseños reales usan **ambos** modelos — concurrente y secuencial —
en la misma entidad. El reproductor de abajo tiene una entidad pequeña
con dos piezas:

1. Un **medio-sumador** — lógica combinacional pura escrita como dos
   asignaciones concurrentes. Sin reloj, sin proceso.
2. Un **contador de 4 bits** — lógica secuencial con reloj, escrita
   dentro de un proceso con `rising_edge(clk)`.

Ambas piezas viven una al lado de la otra en la misma `architecture`.
Usan modelos de ejecución distintos porque responden preguntas distintas:

| Pregunta | Modelo | Por qué |
|---|---|---|
| ¿Cuál es la suma de `a` y `b` *ahora mismo*? | Concurrente | Sin memoria, sin reloj — compuertas puras. |
| ¿Cuál será la cuenta *después del próximo tick de reloj*? | Secuencial | Necesitamos un registro que guarde estado entre flancos. |

## Los dos modelos de ejecución, trabajando juntos

Cuando presiones play verás:

1. Las entradas `a` y `b` cambian → las dos líneas del medio-sumador
   se disparan **juntas** en un solo ciclo delta.
2. El reloj sube → el proceso del contador se despierta y la guarda
   `if rising_edge` nos deja llegar al incremento.
3. El `<= count + 1` del contador es una asignación de señal — se
   confirma cuando termina el delta.

> **Por qué importa en diseños reales:** cada CPU que has usado se
> construyó así. La lógica combinacional decide "¿cuál sería la
> respuesta?", los registros con reloj deciden "¿cuándo la confirmamos?".
> Domina esta división y tienes todo el modelo mental detrás del diseño
> digital.

## Cuando dudes: ¿proceso o concurrente?

- **¿No hay reloj? ¿No necesitas memoria?** Usa asignaciones
  concurrentes. Son más fáciles de leer, más fáciles de razonar y se
  sintetizan a compuertas combinacionales.
- **¿Necesitas recordar un valor entre flancos de reloj?** Usa un
  proceso guardado por `rising_edge(clk)`. Así obtienes registros,
  FSMs, contadores, acumuladores — todo lo que tiene estado.

El reproductor llegó al final. Ya viste secuencial, concurrente, por
qué el paralelo real no aplica, las trampas de la lista de sensibilidad
y los dos modelos conviviendo. Marca la lección como completada
abajo — ya tienes el modelo mental.
