# Secuencial: un paso a la vez

Dentro del cuerpo de un `process`, VHDL se comporta muy parecido a los lenguajes que ya conoces: las sentencias se ejecutan **de arriba abajo, en el orden del código fuente**. Mueve una línea de lugar y cambias el resultado.

Pero hay un detalle que atrapa a todos: las **variables** y las **señales** se comportan distinto.

| Tipo | Sintaxis | ¿Cuándo aparece el nuevo valor? |
|---|---|---|
| **Variable** | `tmp := a + 1` | **Inmediatamente.** La siguiente sentencia ya lee el nuevo `tmp`. |
| **Señal** | `result <= tmp + b` | **Al final del ciclo delta.** Hasta entonces, las lecturas siguen viendo el valor viejo. |

El reproductor recorre un proceso minúsculo que usa ambos. Observa cómo `tmp` se actualiza en el mismo momento en que ejecutamos la línea 5, pero `result` no cambia hasta *después* de que el proceso termina.

<!-- exec-fig2:start -->
## La libreta y el buzón

Una **variable** es como una libreta en el bolsillo del trabajador: cuando corre la línea 4 o la 5, el nuevo número se anota de inmediato y la línea siguiente ya lo lee. Una **señal** es como una carta: `result <= tmp + b` deja la carta (13) en la bandeja de salida, pero `result` sigue mostrando U hasta que pasa el **camión del correo** —el fin del ciclo delta, marcado con δ— y la entrega.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-mailbox.html" title="Escritorio con una libreta (variable) y una bandeja de salida, un camión de correo y un buzón (señal)" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

La figura recorre los mismos pasos que el reproductor de abajo. Haz clic en los puntos bajo el código para saltar a un paso, o en el dibujo para pausar.
<!-- exec-fig2:end -->

## Por qué importa

Imagina que escribieras:

```vhdl
result <= 1;
result <= 2;
```

En C, después de estas dos líneas `result` claramente sería `2`. ¿En VHDL? **Gana la última asignación** — pero solo cuando se cierra el delta. Mientras el proceso corre, `result` sigue con su valor viejo, y el simulador descarta silenciosamente la transacción `<= 1` porque `<= 2` la reemplaza antes de que termine el delta.

> **Prueba esto:** avanza hasta la línea 7 y mira la tabla de señales. `tmp` es `8`, pero `result` sigue siendo `U`. Avanza una vez más y observa cómo `result` salta a `13` *después* de que el cuerpo del proceso ha terminado.
