# Concurrente: todo se dispara a la vez

Fuera de cualquier proceso — directamente dentro de una `architecture`, entre `begin` y `end` — puedes escribir **asignaciones de señal concurrentes**. Se ven como las asignaciones secuenciales pero siguen una regla completamente distinta:

> **Todas las sentencias concurrentes que sean sensibles a un evento se disparan en el mismo ciclo delta.** El orden en el código fuente es irrelevante.

Imagina un edificio con cuatro interruptores en habitaciones distintas. Cuando llega la corriente, el estado de cada interruptor surte efecto en el mismo instante — no importa qué habitación cableó primero el electricista.

El reproductor de abajo tiene cuatro sentencias concurrentes que manejan cuatro salidas distintas desde las mismas entradas `a` y `b`. Presiona play y observa cómo las cuatro líneas de salida se actualizan en el mismo paso.

<!-- exec-fig:start -->
## De vuelta al restaurante: revisar vs campanas

Aquí sigue habiendo **una sola mesera**, así que esto es concurrencia, no paralelismo. Compara dos formas en que puede alternar las mesas:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=poll,bell&mode=poll" title="Restaurante: una mesera que revisa todas las mesas vs una que responde a campanas" loading="lazy"></iframe></div>

- **1 ↻** — da vueltas y **revisa** cada mesa, esté lista o no. Cada ↻ del indicador es un viaje desperdiciado.
- **1 <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M3 12H13M4.6 12C4.6 6 5.8 3 8 3S11.4 6 11.4 12M6.6 14.2A1.5 1.5 0 0 0 9.4 14.2"/></svg>** — toma todas las órdenes y después **solo va a donde suena una campana** (la campana de la mesa se enciende cuando su platillo está listo).

El simulador de VHDL funciona de la segunda forma. No vuelve a ejecutar todas las sentencias todo el tiempo: solo despierta los procesos cuya **lista de sensibilidad** recibió un **evento**. La campana es la lista de sensibilidad; que suene es el evento. Misma mesera, mismas mesas, menos pasos (unos 23 s en lugar de 37 s, y 0 revisiones desperdiciadas).
<!-- exec-fig:end -->

<!-- exec-fig2:start -->
## El fotógrafo: una foto, un flash

¿Por qué no importa el orden del código? Porque en un ciclo delta todas las sentencias concurrentes leen la misma **foto** de las entradas, y nadie ve el resultado de otra hasta el **flash**.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-photo.html" title="Estudio de fotografía: cuatro escritorios copian de una sola foto; el flash voltea los cuatro tableros juntos" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- La cámara dispara: la foto congela `a` y `b`.
- Cada escritorio (uno por sentencia) escribe su **borrador** a partir de esa misma foto.
- El flash es el fin del delta: los cuatro tableros de salida cambian **al mismo tiempo**.
<!-- exec-fig2:end -->

## Dos formas de comprobarlo

- **Intercambia mentalmente dos líneas** — ¿cambia el resultado? No. Inténtalo.
- **Quita las entradas de la arquitectura y mira qué pasa con TODAS las salidas.** Todas vuelven a `U` juntas, porque no hay secuencia — es una evaluación paralela por ciclo delta.

## Concurrente vs secuencial — ¿cuándo usar cada uno?

| Caso de uso | Elige |
|---|---|
| Lógica combinacional (compuertas, mux, aritmética sin reloj) | **Concurrente** |
| Comportamiento con reloj (registros, FSM, contadores) | **Secuencial** (proceso con `rising_edge(clk)`) |
| Leer varias señales, hacer matemáticas, sacar un resultado | Cualquiera funciona — concurrente se lee más como un circuito, secuencial más como un programa |

> **Prueba esto:** pausa el reproductor en el momento en que las cuatro líneas están resaltadas. Ese único momento es **un ciclo delta**. Las cuatro asignaciones se están evaluando exactamente al mismo tiempo de simulación.
