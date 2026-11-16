# ¿Qué significa "ejecutar"?

Cuando presionas **Ejecutar** en Kmila, el simulador no enciende realmente una FPGA. Recorre tu VHDL paso a paso — pero **cómo** lo recorre depende de dónde vive el código.

Hay tres modelos de ejecución que la gente confunde todo el tiempo:

| Modelo | Significado | Dónde aparece en VHDL |
|---|---|---|
| **Secuencial** | Una sentencia a la vez, en el orden del código. | Dentro del cuerpo de un `process`. |
| **Concurrente** | Muchas sentencias se evalúan "a la vez" — el orden en el archivo es irrelevante. | Sentencias concurrentes en una arquitectura (entre `begin` y `end`). |
| **Paralelo** | Varias cosas sucediendo realmente al mismo instante de tiempo real. | **Solo en hardware real.** Nunca en simulación. |

El ejemplo de abajo es deliberadamente pequeño — un proceso que lee dos entradas `a` y `b` y escribe una salida `sum`. Presiona ▶ en el reproductor y observa qué línea está "mirando" el simulador en cada momento.

```text
Secuencial:   línea 1  →  línea 2  →  línea 3  …
Concurrente:  las líneas 1, 2 y 3 se evalúan este mismo instante
Paralelo:     no existe en simulación. Solo ocurre en la FPGA misma.
```

<!-- exec-fig:start -->
## La analogía del restaurante

Imagina un restaurante con **10 mesas**. La **mesera** es quien hace el trabajo (un CPU, o el simulador de Kmila). Cada **mesa** es un trabajo que necesita atención, y cada **platillo** tarda un rato en cocinarse. Usa los botones sobre el dibujo para cambiar cómo se atiende el restaurante:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=seq,poll,par" title="Restaurante: una mesera, una mesera que alterna mesas, cinco meseras" loading="lazy"></iframe></div>

- **1 →** — *secuencial*: una mesera atiende una mesa a la vez. Toma la orden y **se queda ahí hasta que llega el platillo**, aunque otras nueve mesas estén esperando.
- **1 ↻** — *concurrente*: sigue siendo una mesera, pero si el platillo no está listo **pasa a la siguiente**: toma la siguiente orden, regresa a revisar la primera y sirve lo que ya esté listo.
- **5 ‖** — *paralelo*: cinco meseras, dos mesas cada una, todas trabajando **en el mismo instante**.

Cómo leer la gráfica: una fila por mesa; **barra azul** = la mesera está en esa mesa, **línea roja** = el platillo se está cocinando, **punteado** = el platillo está listo pero aún no se sirve, **punto verde** = servido. El indicador muestra <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><circle cx="8" cy="9" r="5.5"/><path d="M8 9V6.2M6.5 1.8H9.5M8 1.8V3.5"/></svg> tiempo transcurrido, <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M4.5 2H11.5M4.5 14H11.5M5.2 2C5.2 5.5 10.8 6.8 10.8 14M10.8 2C10.8 5.5 5.2 6.8 5.2 14"/></svg> tiempo que la mesera estuvo parada sin hacer nada, ↻ revisiones de "¿ya está?" y ✓ mesas servidas. Al terminar cada modo queda una línea punteada de meta en la gráfica para comparar los modos.

| Restaurante | Modelo de ejecución |
|---|---|
| Mesera | Quien ejecuta: un hilo del CPU, o el simulador |
| Mesa | Un `process` (o una sentencia concurrente) |
| Platillo cocinándose | Tiempo que pasa (`after 10 ns`, esperar un flanco de reloj…) |
| Platillo listo | Un **evento**: ocurrió algo que el proceso estaba esperando |
| Cinco meseras | Hardware real: una FPGA, donde cada parte del circuito es su propia "mesera" |

> **Observa:** la mesera concurrente no camina más rápido; simplemente **nunca se queda parada**. Lo secuencial tarda unos 70 s, lo concurrente unos 37 s y lo paralelo unos 12 s.
<!-- exec-fig:end -->

## Por qué importa

Los principiantes leen `sum <= b;` y piensan que el valor de `sum` se vuelve `b` **inmediatamente**, como sería `sum = b;` en C. No lo hace. La asignación **registra una transacción** y el nuevo valor solo es visible después de que termina el ciclo delta actual.

El reproductor de la derecha tiene una ejecución hardcodeada — no hay una simulación real corriendo. Eso mantiene honesta la explicación: cada paso lleva una nota didáctica en lugar de hacerte adivinar qué estaba pensando el simulador.

> **Prueba esto:** avanza manualmente y observa cómo `sum` se queda en `U` ("no inicializado") incluso después del paso 3 — solo cambia cuando el ciclo delta se cierra en el paso 4.
