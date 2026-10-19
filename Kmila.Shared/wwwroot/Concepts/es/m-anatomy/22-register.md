# Registro

Un **registro** es un banco de flip-flops que guarda una palabra entera a la vez. En el flanco del reloj (al cargar) todos los bits de D se capturan juntos en Q.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/22-register.html" title="Bloque secuencial multibit interactivo, gobernado por reloj" loading="lazy"></iframe></div>

Un reloj libre marca a la izquierda; el estado de 4 bits se muestra como una fila de **lámparas Q** y como valor en vivo (decimal + binario). En cada flanco el estado se actualiza; entre flancos se mantiene.

En VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if ld = '1' then q <= d; end if;  -- load on edge
  end if;
end process;
```

En los registros una CPU guarda sus valores de trabajo, y cada etapa de pipeline es un registro. Pon un MUX delante (valor nuevo vs. Q actual) y la habilitación de carga se vuelve mantener.
