# Cerrojo D (latch)

Un **cerrojo D** guarda un bit. Mientras su habilitación (aquí el reloj) está en alto es **transparente** — Q sigue a D; cuando baja, **congela** el último valor. Es el primer elemento **secuencial** de la ruta: a diferencia de una compuerta, su salida depende del reloj y de su propio pasado, no solo de las entradas actuales.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/18-latch.html" title="Elemento secuencial interactivo con diagrama de tiempos en vivo" loading="lazy"></iframe></div>

Este elemento tiene **memoria**. Observa el diagrama de tiempos: el reloj (**CLK**) marca arriba, y la salida **Q** solo cambia en el reloj — entre medias **mantiene** su valor pase lo que pase en las entradas. Q se dibuja en color; las líneas punteadas marcan los flancos del reloj.

En VHDL:

```vhdl
process (en, d)
begin
  if en = '1' then   -- transparent while enabled
    q <= d;
  end if;
end process;
```

Al ser **sensible al nivel**, un cerrojo es transparente durante toda una fase del reloj — cualquier glitch en D en esa ventana pasa. Por eso el diseño síncrono prefiere flip-flops por flanco.
