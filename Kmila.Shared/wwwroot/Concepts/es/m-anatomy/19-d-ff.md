# Flip-flop D

Un **flip-flop D** guarda un bit, capturado en el **flanco de subida** del reloj. Q toma el valor de D en el instante del flanco y lo mantiene hasta el siguiente. Es el primer elemento **secuencial** de la ruta: a diferencia de una compuerta, su salida depende del reloj y de su propio pasado, no solo de las entradas actuales.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/19-d-ff.html" title="Elemento secuencial interactivo con diagrama de tiempos en vivo" loading="lazy"></iframe></div>

Este elemento tiene **memoria**. Observa el diagrama de tiempos: el reloj (**CLK**) marca arriba, y la salida **Q** solo cambia en el reloj — entre medias **mantiene** su valor pase lo que pase en las entradas. Q se dibuja en color; las líneas punteadas marcan los flancos del reloj.

En VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= d;          -- sampled on the edge
  end if;
end process;
```

El flip-flop D es **el** ladrillo del diseño síncrono — cada registro, etapa de pipeline y bit de estado es uno o más flip-flops D. Pon un MUX delante de D y obtienes habilitación de carga o reset síncrono.
