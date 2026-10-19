# El hardware es paralelo

Quienes vienen de C, Python o JavaScript traen interiorizado el mismo modelo mental: "el código se ejecuta de arriba abajo, una sentencia a la vez". VHDL te va a castigar por esa suposición hasta que construyas el modelo mental correcto. Este es el salto conceptual más grande de toda la unidad de HDL, así que vale la pena ir despacio.

## El gran cambio

Mira esta arquitectura:

```vhdl
architecture rtl of demo is
begin
  y1 <= a and b;
  y2 <= a or  b;
  y3 <= y1 xor y2;
end architecture rtl;
```

En un lenguaje de software leerías esto como tres pasos secuenciales. En VHDL, las tres líneas describen **cables que existen al mismo tiempo**. No se ejecutan en orden — existen.

El sintetizador construye:

```mermaid
flowchart LR
    A([a]) --> AND["AND"]
    B([b]) --> AND
    A --> OR["OR"]
    B --> OR
    AND -- y1 --> XOR["XOR"]
    OR  -- y2 --> XOR
    XOR --> Y3([y3])
```

Las tres compuertas están encendidas al mismo tiempo. Cuando `a` o `b` cambia, `y3` se estabiliza en su nuevo valor tras un pequeño retraso de propagación por el camino que resulte más largo. No hay "primero" ni "segundo".

## Por qué el orden no importa

Podrías escribir las tres líneas en cualquier orden:

```vhdl
y3 <= y1 xor y2;   -- ¿"referencia antes de declaración"?
y1 <= a and b;
y2 <= a or  b;
```

Esto sigue siendo VHDL válido y produce el circuito idéntico. Las tres asignaciones describen tres cables, y a los cables no les importa el orden en que dibujaste el esquemático. La herramienta lee las tres y los conecta.

## El motor orientado a eventos detrás de todo

Entonces, ¿cómo simula una computadora *secuencial* todo esto "a la vez"? Cada asignación concurrente se comporta como un pequeño proceso con una lista de sensibilidad implícita: se **reevalúa cada vez que cambia cualquier señal de su lado derecho**. El simulador mantiene una cola de eventos pendientes; cuando `a` cambia despierta a toda asignación que lea `a`, esas recalculan y pueden agendar nuevos eventos, y así hasta que el circuito queda en reposo. Esa "estabilización" es la razón por la que la concurrencia es determinista en vez de caótica — un punto que la siguiente lección (delta-ciclos) hace preciso.

## Dónde vuelve a colarse lo "secuencial"

Dentro de un `process`, las cosas **sí** son secuenciales — dentro de ese proceso, las sentencias corren una a la vez, de arriba abajo. Pero un proceso *como un todo* sigue siendo una unidad concurrente más al lado de las demás.

```vhdl
architecture rtl of demo is
begin

  -- Concurrente 1
  y1 <= a and b;

  -- Concurrente 2 (un proceso, que internamente es secuencial)
  process(clk)
  begin
    if rising_edge(clk) then
      counter <= counter + 1;
    end if;
  end process;

  -- Concurrente 3
  y2 <= a or b;

end architecture rtl;
```

Las tres unidades concurrentes están vivas al mismo tiempo. Cada una se ejecuta cuando cambian sus entradas. Dentro del proceso, el cuerpo se lee en orden, pero es una propiedad local del proceso, no de la arquitectura. De hecho, una asignación de señal concurrente es *exactamente* una forma abreviada de un proceso de una sola línea — las dos son intercambiables.

## Una consecuencia concreta: los lazos combinacionales

Este diseño tiene un bug sutil:

```vhdl
architecture wrong of demo is
  signal a, b : std_logic;
begin
  a <= not b;
  b <= not a;
end architecture wrong;
```

Leído como software, parece "pon `a` en no-b, luego pon `b` en no-a" — sin problema. Leído como hardware, son dos inversores cableados en un lazo sin ningún registro que lo rompa. Eso es un **lazo combinacional**: un anillo de retroalimentación que nunca puede estabilizarse. Según los retrasos, o bien oscila o bien se engancha en un estado impredecible. La simulación girará entre delta-ciclos (y puede reportar un error de límite de iteraciones), las herramientas de síntesis advertirán de un lazo combinacional, y la placa tendrá glitches.

> El arreglo siempre es el mismo: rompe la retroalimentación con un registro. Si *quieres* memoria, pasa el lazo por un proceso con reloj (así es como se construye un flip-flop). Si no, tienes un error de diseño.

## Cuenta el hardware

Un hábito útil mientras recableas tu cerebro: lee una arquitectura y cuenta las compuertas. El primer ejemplo tiene 3 compuertas (AND, OR, XOR). Este —

```vhdl
y <= (a and b) or (c and d) or (e and f);
```

— son tres compuertas AND alimentando una OR de 3 entradas, todas vivas a la vez. Ya no estás contando *pasos*; estás contando *partes*.

## El recableado en tu cabeza

- Deja de pensar "¿qué se ejecuta primero?"
- Empieza a pensar "¿qué cables existen y qué alimenta a qué?"
- Dentro de un proceso vuelve el viejo modelo "uno a la vez" — pero solo dentro de ese proceso.
- Nunca cierres un camino combinacional sobre sí mismo sin un registro.

Una vez que esto hace clic, VHDL deja de pelearse contigo. Lees una arquitectura y ves de inmediato el esquemático que representa, no la "ejecución del programa" que solías imaginar.

## Qué recordar

- Las sentencias concurrentes describen cables que existen todos a la vez; su orden en el archivo es irrelevante.
- El simulador finge esto con una cola de eventos — las asignaciones se vuelven a ejecutar cada vez que cambia una entrada.
- Un `process` es una unidad concurrente; solo *dentro* de él el código es secuencial.
- Un lazo de retroalimentación combinacional sin registro es un bug — rómpelo con un proceso con reloj.
