# Delta-ciclos y condiciones de carrera

Un simulador tiene que dar a "dos cosas pasando al mismo tiempo" un orden determinista. La respuesta de VHDL es el **delta-ciclo**: una rebanada infinitesimal de tiempo que separa causa y efecto.

## Qué es un delta-ciclo

El tiempo de simulación avanza en pasos reales (medidos en ns, µs, ms). Pero dentro de un mismo paso de tiempo, los cambios ocurren en orden de **delta-ciclo**:

- δ₀: programas una actualización de señal.
- δ₁: la actualización toma efecto; todo proceso sensible a esa señal ya tiene nuevas entradas.
- δ₂: esos procesos programan sus actualizaciones.
- δ₃: esas actualizaciones toman efecto.
- …

Cada delta es "cero tiempo real" pero representa un paso causal. El simulador sigue iterando deltas hasta que nada más tiene que actualizarse — entonces y solo entonces el tiempo real avanza.

## Por qué existe

Imagina dos señales que se actualizan en el mismo flanco:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

Sin delta-ciclos, el simulador tendría que elegir un orden arbitrario — ¿se actualiza `a` antes o después que `b`? ¿Al mismo instante exacto? Los deltas eluden esto: ambas asignaciones se programan en δ₀ y ambas toman efecto en δ₁. Cualquier proceso que lea `a` o `b` ve los nuevos valores a la vez.

## La trampa de la condición de carrera

Mira esto:

```vhdl
process(a)
begin
  b <= a;
end process;

process(b)
begin
  c <= b;
end process;
```

Cuando `a` cambia, el primer proceso dispara y programa `b`. En δ₁, `b` tiene su nuevo valor. El segundo proceso dispara, programa `c`. En δ₂, `c` está correcto.

Ahora intercambia los dos procesos. ¿Cambia algo? **No.** Los delta-ciclos garantizan que ambos caminos producen el mismo resultado final — aunque el "orden" en el archivo sea distinto. Para eso existen los deltas.

## Dónde fallan las cosas

El problema aparece cuando mezclas semántica de **signal** con "necesito que esto pase antes que aquello dentro del mismo tick". Ejemplo:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a se actualiza al final del tick
    b <= a + 1;     -- b lee el a VIEJO
  end if;
end process;
```

Si asumías "la línea 1 corre primero, así que `a` ya está actualizada cuando corre la línea 2", te equivocas. `a` se actualiza al final del tick. `b` lee el `a` viejo. Resultado: ambos se incrementan con datos viejos, no con los nuevos.

Reemplaza las signals con variables para recuperar la semántica de C:

```vhdl
process(clk)
  variable aa, bb : integer;
begin
  if rising_edge(clk) then
    aa := bb + 1;   -- inmediato
    bb := aa + 1;   -- ve el nuevo aa
  end if;
end process;
```

Ahora funciona como un programador de software esperaba.

## Para llevar

- Los delta-ciclos son cómo el simulador hace determinista la lógica concurrente.
- Las asignaciones de signal toman efecto **después** de los deltas. Las variables no.
- Si tu testbench se porta "raro" cerca de un flanco, sospecha de un orden de deltas antes de culpar a la herramienta.

El siguiente ejercicio del plan muestra una pequeña carrera en acción. Encuéntrala, nómbrala, arréglala — y la regla no se te olvida más.
