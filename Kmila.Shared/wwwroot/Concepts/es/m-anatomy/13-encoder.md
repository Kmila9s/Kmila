# Codificador

Un **codificador** es el inverso del decodificador: dada una entrada one-hot (una línea activa), entrega la **dirección binaria** de esa línea.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/13-encoder.html" title="Bloque combinacional interactivo sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. Los émbolos fijan los bits, el módulo calcula, las lámparas de salida se encienden y la **fila actual de la tabla se resalta** para leer la función directamente.

En VHDL:

```vhdl
a <= "11" when i3 = '1' else
     "10" when i2 = '1' else
     "01" when i1 = '1' else
     "00";
```

Es puramente **combinacional**: las salidas dependen solo de las entradas actuales, sin reloj ni memoria. Un codificador con **prioridad** resuelve varias entradas activas tomando la mayor — útil en controladores de interrupciones que deben elegir una petición.
