# Compuerta XNOR (equivalencia)

La compuerta **XNOR** da 1 cuando sus entradas son **iguales** — es un XOR con burbuja de inversión.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/09-xnor.html" title="Compuerta lógica interactiva sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. El módulo calcula el resultado y la lámpara de salida **Y** se enciende según la tabla.

En VHDL:

```vhdl
y <= a xnor b;
```

Es puramente **combinacional**: la salida depende solo de las entradas actuales, sin reloj ni memoria. XNOR responde «¿son iguales estos dos bits?» — es la función de igualdad (comparador) de 1 bit.
