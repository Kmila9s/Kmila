# Sumador completo

Un **sumador completo** suma tres bits — A, B y un acarreo de entrada **Ci** — produciendo una suma S y un acarreo de salida **Co**. Encadenados, suman números anchos.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/15-full-adder.html" title="Bloque combinacional interactivo sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. Los émbolos fijan los bits, el módulo calcula, las lámparas de salida se encienden y la **fila actual de la tabla se resalta** para leer la función directamente.

En VHDL:

```vhdl
s  <= a xor b xor ci;
co <= (a and b) or (ci and (a xor b));
```

Es puramente **combinacional**: las salidas dependen solo de las entradas actuales, sin reloj ni memoria. El sumador completo es el ladrillo de todo sumador binario: encadena n, acarreo con acarreo, y sumas dos números de n bits. Se arma con dos semisumadores y un OR.
