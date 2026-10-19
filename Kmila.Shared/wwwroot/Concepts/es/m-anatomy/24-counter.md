# Contador

Un **contador** suma uno a su valor guardado en cada flanco, recorriendo 0, 1, 2, … y dando la vuelta. Es un registro con un sumador +1 realimentado.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/24-counter.html" title="Bloque secuencial multibit interactivo, gobernado por reloj" loading="lazy"></iframe></div>

Un reloj libre marca a la izquierda; el estado de 4 bits se muestra como una fila de **lámparas Q** y como valor en vivo (decimal + binario). En cada flanco el estado se actualiza; entre flancos se mantiene.

En VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q + 1;   -- increment every edge
  end if;
end process;
```

Los contadores miden tiempo y eventos, generan direcciones y dividen relojes. Observa el bit más bajo conmutar en cada flanco y cada bit superior a la mitad del ritmo — conteo binario en movimiento.
