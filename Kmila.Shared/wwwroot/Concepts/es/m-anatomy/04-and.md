# Compuerta AND

La compuerta **AND** da 1 solo cuando **ambas** entradas son 1.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/04-and.html" title="Compuerta lógica interactiva sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. El módulo calcula el resultado y la lámpara de salida **Y** se enciende según la tabla.

En VHDL:

```vhdl
y <= a and b;
```

Es puramente **combinacional**: la salida depende solo de las entradas actuales, sin reloj ni memoria. Piensa en dos interruptores en serie: la corriente llega a la lámpara solo si ambos están cerrados.
