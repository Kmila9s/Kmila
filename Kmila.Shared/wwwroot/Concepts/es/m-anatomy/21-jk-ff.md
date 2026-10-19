# Flip-flop JK

Un **flip-flop JK** es el más general: en cada flanco J pone Q a 1, K la pone a 0, ambos a la vez la **conmutan**, y ninguno la mantiene. Es el primer elemento **secuencial** de la ruta: a diferencia de una compuerta, su salida depende del reloj y de su propio pasado, no solo de las entradas actuales.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/21-jk-ff.html" title="Elemento secuencial interactivo con diagrama de tiempos en vivo" loading="lazy"></iframe></div>

Este elemento tiene **memoria**. Observa el diagrama de tiempos: el reloj (**CLK**) marca arriba, y la salida **Q** solo cambia en el reloj — entre medias **mantiene** su valor pase lo que pase en las entradas. Q se dibuja en color; las líneas punteadas marcan los flancos del reloj.

En VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if    j='1' and k='1' then q <= not q;
    elsif j='1' then q <= '1';
    elsif k='1' then q <= '0';
    end if;
  end if;
end process;
```

El JK elimina el estado prohibido del antiguo cerrojo SR. Cualquier flip-flop puede construirse a partir de otro — D, T y JK son intercambiables con algo de lógica delante.
