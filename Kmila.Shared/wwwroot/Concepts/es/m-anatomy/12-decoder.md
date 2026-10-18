# Decodificador

Un **decodificador** convierte una dirección de n bits en una salida **one-hot**: exactamente una de sus 2ⁿ salidas vale 1, la que nombra la dirección.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/12-decoder.html" title="Bloque combinacional interactivo sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. Los émbolos fijan los bits, el módulo calcula, las lámparas de salida se encienden y la **fila actual de la tabla se resalta** para leer la función directamente.

En VHDL:

```vhdl
with a select y <=
  "0001" when "00",
  "0010" when "01",
  "0100" when "10",
  "1000" when others;
```

Es puramente **combinacional**: las salidas dependen solo de las entradas actuales, sin reloj ni memoria. Los decodificadores manejan líneas de dirección: memorias, displays de siete segmentos y el despacho de instrucciones eligen así una línea entre muchas.
