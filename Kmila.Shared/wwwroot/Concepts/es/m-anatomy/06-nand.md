# Compuerta NAND

La compuerta **NAND** es un AND seguido de un NOT: da 0 solo cuando ambas entradas son 1.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/06-nand.html" title="Compuerta lógica interactiva sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. El módulo calcula el resultado y la lámpara de salida **Y** se enciende según la tabla.

En VHDL:

```vhdl
y <= a nand b;
```

Es puramente **combinacional**: la salida depende solo de las entradas actuales, sin reloj ni memoria. La burbuja de la salida marca la inversión. **NAND es universal**: cualquier función lógica puede construirse solo con compuertas NAND.
