# Demultiplexor (DEMUX)

Un **demultiplexor** es el inverso del MUX: encamina una entrada **D** hacia una de varias salidas, elegida por la línea de **selección** S.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/11-demux.html" title="Bloque combinacional interactivo sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. Los émbolos fijan los bits, el módulo calcula, las lámparas de salida se encienden y la **fila actual de la tabla se resalta** para leer la función directamente.

En VHDL:

```vhdl
y0 <= d when s = '0' else '0';
y1 <= d when s = '1' else '0';
```

Es puramente **combinacional**: las salidas dependen solo de las entradas actuales, sin reloj ni memoria. Las salidas no seleccionadas quedan en 0. Decodificadores y demultiplexores son primos: un demux es un decodificador cuya entrada de datos habilita la salida activa.
