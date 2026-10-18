# Comparador

Un **comparador** compara dos números A y B e indica cuál es el caso: A < B, A = B o A > B.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/16-comparator.html" title="Bloque combinacional interactivo sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. Los émbolos fijan los bits, el módulo calcula, las lámparas de salida se encienden y la **fila actual de la tabla se resalta** para leer la función directamente.

En VHDL:

```vhdl
lt <= '1' when a < b else '0';
eq <= '1' when a = b else '0';
gt <= '1' when a > b else '0';
```

Es puramente **combinacional**: las salidas dependen solo de las entradas actuales, sin reloj ni memoria. Exactamente una de las tres salidas vale 1. Los comparadores de magnitud escalan bit a bit, del más significativo hacia abajo.
