# Semisumador

Un **semisumador** suma dos bits A y B, produciendo una **suma** S y un **acarreo** C.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/14-half-adder.html" title="Bloque combinacional interactivo sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. Los émbolos fijan los bits, el módulo calcula, las lámparas de salida se encienden y la **fila actual de la tabla se resalta** para leer la función directamente.

En VHDL:

```vhdl
s <= a xor b;   -- sum
c <= a and b;   -- carry
```

Es puramente **combinacional**: las salidas dependen solo de las entradas actuales, sin reloj ni memoria. La suma es un **XOR** y el acarreo un **AND**: un semisumador son literalmente esas dos compuertas juntas. No tiene acarreo de entrada, así que es solo la primera etapa.
