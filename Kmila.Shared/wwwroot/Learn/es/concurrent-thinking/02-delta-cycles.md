# Delta-ciclos y condiciones de carrera

Un simulador necesita darle a "dos cosas pasando al mismo tiempo" un orden determinista. La respuesta de VHDL es el **delta-ciclo**: una rebanada infinitesimal de tiempo que separa la causa del efecto sin mover el reloj de la pared. Está definido con precisión en el ciclo de simulación del estándar VHDL, y entenderlo es lo que hace que el comportamiento "raro" de un testbench de pronto tenga sentido.

## Qué es un delta-ciclo

El tiempo de simulación avanza en pasos reales (medidos en ns, µs, ms). Pero dentro de un mismo paso de tiempo, los cambios ocurren en orden de **delta-ciclo**:

- δ₀: una asignación agenda una actualización de señal.
- δ₁: la actualización toma efecto; todo proceso sensible a esa señal ya ve nuevas entradas y despierta.
- δ₂: esos procesos despiertos agendan *sus* actualizaciones.
- δ₃: esas actualizaciones toman efecto; más procesos pueden despertar.
- …

Cada delta es "cero tiempo real" pero representa un paso causal. El simulador sigue iterando deltas hasta que ninguna señal cambia más — el circuito está **estable** — y solo entonces el tiempo de simulación real avanza al siguiente evento.

## El ciclo de simulación, con un poco más de precisión

Una pasada por el motor es: (1) avanzar el tiempo al siguiente evento agendado; (2) actualizar todas las señales agendadas para ahora; (3) ejecutar hasta el final cada proceso que estaba esperando una de esas señales, lo que puede agendar nuevas actualizaciones; (4) si alguna de esas es para el tiempo *actual*, hacer otro delta-ciclo; si no, avanzar el tiempo. Las actualizaciones de señal del paso 2 ocurren todas *antes* de que cualquier proceso las lea en el paso 3 — ese es el mecanismo detrás de "las señales se actualizan al final del paso".

## Por qué existe

Considera dos señales que se actualizan en el mismo flanco de reloj:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

Sin delta-ciclos, el simulador tendría que elegir un orden arbitrario — ¿se actualiza `a` antes que `b`? ¿Después? Los delta-ciclos esquivan esto: ambas asignaciones se agendan en δ₀ y ambas toman efecto en δ₁. Cualquier proceso que lea `a` o `b` ve ambos valores nuevos juntos. El resultado ya no depende del orden en que el simulador resulte visitar las cosas.

## Una cadena de flujo de datos

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

Cuando `a` cambia, el primer proceso dispara y agenda `b`. En el siguiente delta, `b` tiene su nuevo valor, que despierta al segundo proceso, que agenda `c`. En el delta siguiente, `c` está correcto. Nota que `c` alcanza a `a` después de **dos delta-ciclos** — el tiempo real nunca se movió, pero la causalidad se preservó en orden. Intercambia los dos procesos en el archivo y nada cambia: el orden de deltas garantiza el mismo resultado final. Justo para eso existen los delta-ciclos.

## La trampa de la condición de carrera

El problema es cuando mezclas la semántica de **señal** con la suposición de que "esto tiene que pasar antes que aquello dentro del mismo tick":

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a se actualiza al final del paso
    b <= a + 1;     -- b lee el a VIEJO
  end if;
end process;
```

Si asumiste "la línea 1 corre primero, así que `a` ya está actualizada cuando corre la línea 2", te equivocaste. Ambos lados derechos se evalúan usando los valores **viejos**, y luego ambos resultados se agendan. `b` lee el valor viejo de `a`, no el recién calculado.

Reemplaza las señales con variables para obtener la semántica estilo C, de leer-lo-que-acabas-de-escribir:

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

Ahora funciona como esperan los programadores de software. (Y el hardware difiere: la versión con señales hace dos sumadores paralelos a partir de los registros viejos; la versión con variables hace una cadena dependiente.)

## Un bug real clásico: el flanco olvidado

El pensar en deltas también explica por qué este estímulo de testbench se porta mal:

```vhdl
clk  <= not clk after 10 ns;
-- en otro lado, en un proceso sin wait:
data <= next_value;   -- ¿el mismo delta que el flanco que el DUT muestrea?
```

Si una señal de estímulo cambia en el *mismo* delta en que el DUT recibe el flanco, que el DUT vea el valor viejo o el nuevo es frágil. El arreglo es cambiar el estímulo con un margen seguro *lejos* del flanco activo (o usar `wait until rising_edge(clk)` y luego un pequeño retraso). Muchos reportes de "pasa en sim pero el orden se siente aleatorio" se rastrean hasta aquí.

## Detectar deltas en las herramientas

GHDL (el motor que maneja Kmila) y GTKWave exponen ambos los pasos delta — una señal puede tardar varios delta-ciclos en propagarse a su valor final en un mismo punto de tiempo. Si una forma de onda muestra un glitch que ocupa "cero tiempo", estás viendo valores delta intermedios, no comportamiento real del hardware.

## Para llevar

- Los delta-ciclos son la forma en que el simulador hace determinista la lógica concurrente sin inventar tiempo falso.
- Las asignaciones de señal toman efecto **después** del delta actual; las variables no — son inmediatas.
- Los lados derechos se evalúan con los valores *viejos* de las señales, y luego se agendan.
- Si tu testbench se porta "raro" cerca de un flanco de reloj, sospecha de un problema de orden de deltas antes de culpar a la herramienta.

## Qué recordar

- Un delta-ciclo es cero tiempo real pero un paso causal; el simulador corre deltas hasta que el circuito está estable, y luego avanza el tiempo.
- Las señales se actualizan entre deltas (por eso lees valores viejos en el mismo paso); las variables se actualizan al instante.
- Aleja el estímulo del testbench del flanco de reloj activo para evitar carreras.

El siguiente ejercicio muestra una pequeña condición de carrera en acción. Encuéntrala, nómbrala, arréglala — y nunca olvidarás la regla.
