# ¿Qué significa "ejecutar"?

Cuando presionas **Ejecutar** en Kmila, el simulador no enciende realmente
una FPGA. Recorre tu VHDL paso a paso — pero **cómo** lo recorre depende
de dónde vive el código.

Hay tres modelos de ejecución que la gente confunde todo el tiempo:

| Modelo | Significado | Dónde aparece en VHDL |
|---|---|---|
| **Secuencial** | Una sentencia a la vez, en el orden del código. | Dentro del cuerpo de un `process`. |
| **Concurrente** | Muchas sentencias se evalúan "a la vez" — el orden en el archivo es irrelevante. | Sentencias concurrentes en una arquitectura (entre `begin` y `end`). |
| **Paralelo** | Varias cosas sucediendo realmente al mismo instante de tiempo real. | **Solo en hardware real.** Nunca en simulación. |

El ejemplo de abajo es deliberadamente pequeño — un proceso que lee dos
entradas `a` y `b` y escribe una salida `sum`. Presiona ▶ en el
reproductor y observa qué línea está "mirando" el simulador en cada
momento.

```text
Secuencial:   línea 1  →  línea 2  →  línea 3  …
Concurrente:  las líneas 1, 2 y 3 se evalúan este mismo instante
Paralelo:     no existe en simulación. Solo ocurre en la FPGA misma.
```

## Por qué importa

Los principiantes leen `sum <= b;` y piensan que el valor de `sum` se
vuelve `b` **inmediatamente**, como sería `sum = b;` en C. No lo hace.
La asignación **registra una transacción** y el nuevo valor solo es
visible después de que termina el ciclo delta actual.

El reproductor de la derecha tiene una ejecución hardcodeada — no hay
una simulación real corriendo. Eso mantiene honesta la explicación:
cada paso lleva una nota didáctica en lugar de hacerte adivinar qué
estaba pensando el simulador.

> **Prueba esto:** avanza manualmente y observa cómo `sum` se queda en
> `U` ("no inicializado") incluso después del paso 3 — solo cambia
> cuando el ciclo delta se cierra en el paso 4.
