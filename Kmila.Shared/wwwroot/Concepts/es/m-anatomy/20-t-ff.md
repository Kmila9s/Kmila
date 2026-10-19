# Flip-flop T

Un **flip-flop T conmuta**: en cada flanco, si T = 1 el bit guardado se invierte (0↔1); si T = 0 se mantiene. Divide la frecuencia del reloj entre dos. Es el primer elemento **secuencial** de la ruta: a diferencia de una compuerta, su salida depende del reloj y de su propio pasado, no solo de las entradas actuales.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/20-t-ff.html" title="Elemento secuencial interactivo con diagrama de tiempos en vivo" loading="lazy"></iframe></div>

Este elemento tiene **memoria**. Observa el diagrama de tiempos: el reloj (**CLK**) marca arriba, y la salida **Q** solo cambia en el reloj — entre medias **mantiene** su valor pase lo que pase en las entradas. Q se dibuja en color; las líneas punteadas marcan los flancos del reloj.

En VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if t = '1' then q <= not q; end if;
  end if;
end process;
```

Encadena flip-flops T y cada uno vuelve a dividir entre dos — eso es justo un **contador de rizado** binario. Un T es un D con D conectado a Q xor T.
