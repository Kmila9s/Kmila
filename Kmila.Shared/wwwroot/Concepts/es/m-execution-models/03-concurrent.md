# Concurrente: todo se dispara a la vez

Fuera de cualquier proceso — directamente dentro de una `architecture`,
entre `begin` y `end` — puedes escribir **asignaciones de señal
concurrentes**. Se ven como las asignaciones secuenciales pero siguen
una regla completamente distinta:

> **Todas las sentencias concurrentes que sean sensibles a un evento se
> disparan en el mismo ciclo delta.** El orden en el código fuente es
> irrelevante.

Imagina un edificio con cuatro interruptores en habitaciones distintas.
Cuando llega la corriente, el estado de cada interruptor surte efecto
en el mismo instante — no importa qué habitación cableó primero el
electricista.

El reproductor de abajo tiene cuatro sentencias concurrentes que manejan
cuatro salidas distintas desde las mismas entradas `a` y `b`. Presiona
play y observa cómo las cuatro líneas de salida se actualizan en el
mismo paso.

## Dos formas de comprobarlo

- **Intercambia mentalmente dos líneas** — ¿cambia el resultado? No.
  Inténtalo.
- **Quita las entradas de la arquitectura y mira qué pasa con TODAS las
  salidas.** Todas vuelven a `U` juntas, porque no hay secuencia — es
  una evaluación paralela por ciclo delta.

## Concurrente vs secuencial — ¿cuándo usar cada uno?

| Caso de uso | Elige |
|---|---|
| Lógica combinacional (compuertas, mux, aritmética sin reloj) | **Concurrente** |
| Comportamiento con reloj (registros, FSM, contadores) | **Secuencial** (proceso con `rising_edge(clk)`) |
| Leer varias señales, hacer matemáticas, sacar un resultado | Cualquiera funciona — concurrente se lee más como un circuito, secuencial más como un programa |

> **Prueba esto:** pausa el reproductor en el momento en que las cuatro
> líneas están resaltadas. Ese único momento es **un ciclo delta**. Las
> cuatro asignaciones se están evaluando exactamente al mismo tiempo de
> simulación.
