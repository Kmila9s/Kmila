# Compuerta XOR (OR exclusivo)

La compuerta **XOR** (OR exclusivo) da 1 cuando sus entradas son **distintas**.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/08-xor.html" title="Compuerta lógica interactiva sobre un banco de módulo" loading="lazy"></iframe></div>

Mueve el cursor (o las flechas) para recorrer todas las combinaciones de entrada. El módulo calcula el resultado y la lámpara de salida **Y** se enciende según la tabla.

En VHDL:

```vhdl
y <= a xor b;
```

Es puramente **combinacional**: la salida depende solo de las entradas actuales, sin reloj ni memoria. XOR es el corazón de la suma (el bit de resultado), la paridad y la comparación: responde «¿son distintos estos dos bits?».
