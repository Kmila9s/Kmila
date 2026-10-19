# Registro de desplazamiento

Un **registro de desplazamiento** mueve sus bits un lugar en cada flanco, entrando un bit nuevo por un extremo — una cadena de cubos para los datos.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/23-shift.html" title="Bloque secuencial multibit interactivo, gobernado por reloj" loading="lazy"></iframe></div>

Un reloj libre marca a la izquierda; el estado de 4 bits se muestra como una fila de **lámparas Q** y como valor en vivo (decimal + binario). En cada flanco el estado se actualiza; entre flancos se mantiene.

En VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q(2 downto 0) & si;  -- shift left, SI in
  end if;
end process;
```

Los registros de desplazamiento hacen conversión serie-paralelo (recepción UART), multiplican/dividen por dos y construyen LFSR para secuencias pseudoaleatorias.
