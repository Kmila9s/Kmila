# Compuerta NOR

La compuerta **NOR** es un OR seguido de un NOT: da 1 solo cuando ambas entradas son 0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/07-nor.html" title="Compuerta lógica interactiva sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. El módulo calcula el resultado y la lámpara de salida **Y** se enciende según la tabla.

En VHDL:

```vhdl
y <= a nor b;
```

Es puramente **combinacional**: la salida depende solo de las entradas actuales, sin reloj ni memoria. La burbuja marca la inversión. **NOR también es universal**: cualquier circuito puede construirse solo con compuertas NOR.
