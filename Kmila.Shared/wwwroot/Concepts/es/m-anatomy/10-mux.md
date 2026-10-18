# Multiplexor (MUX)

Un **multiplexor** encamina una de varias entradas de datos hacia la salida. Una línea de **selección** **S** elige cuál: aquí Y = D1 si S = 1, y si no D0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/10-mux.html" title="Bloque combinacional interactivo sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. Los émbolos fijan los bits, el módulo calcula, las lámparas de salida se encienden y la **fila actual de la tabla se resalta** para leer la función directamente.

En VHDL:

```vhdl
y <= d1 when s = '1' else d0;
```

Es puramente **combinacional**: las salidas dependen solo de las entradas actuales, sin reloj ni memoria. Todo `if/then/else` y `case` en VHDL se vuelve un MUX tras la síntesis. También así funciona el load-enable de un registro: un MUX elige entre el valor nuevo y mantener el viejo.
