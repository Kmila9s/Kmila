# Búfer (BUF)

El **búfer** copia su entrada a la salida sin cambiarla (Y = A). No cambia la lógica — se usa para **excitar** una señal: reforzarla o añadir un retardo controlado.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/03-buffer.html" title="Compuerta lógica interactiva sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. El módulo calcula el resultado y la lámpara de salida **Y** se enciende según la tabla.

En VHDL:

```vhdl
y <= a;
```

Es puramente **combinacional**: la salida depende solo de las entradas actuales, sin reloj ni memoria. Fíjate en que no hay burbuja de inversión en la salida — es la única diferencia con una compuerta NOT.
