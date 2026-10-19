# Ejemplo: FSM de semáforo

Un semáforo es la máquina de estados Moore por excelencia: tres estados (`RED`, `GREEN`, `YELLOW`), una salida por color y un temporizador que dice "mantén cada estado por unos cuantos ciclos antes de avanzar".

## La estructura

El ejemplo sigue el **patrón de dos procesos**:

- El Proceso 1 es el **registro de estado** — un flip-flop D clásico que almacena `state` y hace avanzar un contador de permanencia.
- El Proceso 2 es la **lógica de siguiente estado** — puramente combinacional, decide cuál es `next_state` dado el actual.
- Las asignaciones de salida (al final de la arquitectura) son salidas Moore combinacionales — sus valores dependen solo de `state`.

## Paso a paso por el código

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 ticks por color
```

`state_t` es una **enumeración** — el sintetizador elige la codificación binaria real, nosotros solo nos referimos a estados con nombre. `count` lleva la cuenta de cuánto tiempo llevamos en el estado actual. `HOLD` es el tiempo de permanencia.

Registro de estado:

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    state <= S_RED;
    count <= (others => '0');
  elsif rising_edge(clk) then
    if count = HOLD then
      state <= next_state;
      count <= (others => '0');
    else
      count <= count + 1;
    end if;
  end if;
end process;
```

Dos detalles que vale la pena nombrar. Primero, este es un **reset asíncrono** (`reset` está en la lista de sensibilidad y se evalúa antes del `rising_edge`) — el semáforo salta a `S_RED` en el instante en que el reset se activa. Un reset síncrono sacaría a `reset` de la lista de sensibilidad y lo evaluaría solo dentro de la rama del `rising_edge`; ambos son válidos, solo elige un estilo y mantente consistente. Segundo, el contador convierte una transición de estado en una permanencia de 8 ciclos — el estado solo avanza en el ciclo donde `count = HOLD`.

Lógica de siguiente estado:

```vhdl
process(state)
begin
  case state is
    when S_RED    => next_state <= S_GREEN;
    when S_GREEN  => next_state <= S_YELLOW;
    when S_YELLOW => next_state <= S_RED;
  end case;
end process;
```

Como este proceso asigna `next_state` en cada rama y el `case` es exhaustivo sobre `state_t`, aquí no hay latch inferido. La lista de sensibilidad es solo `(state)` — correcto, porque `next_state` no depende de nada más. En el momento en que hagas que el siguiente estado dependa de una entrada, esa entrada tiene que unirse a la lista de sensibilidad (o el proceso combinacional simulará mal), y deberías agregar un valor por defecto `next_state <= state;` en la primera línea como seguro.

Salidas (Moore puro):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

Cada salida es función solo de `state` — así que solo cambian cuando el registro avanza, y no pueden glitchar entre flancos. Ese es todo el atractivo de una máquina Moore para algo que maneja una señal física.

## Qué hace el ejemplo

El reloj corre por unos 400 µs. Cada 8 relojes (7 de permanencia + 1 de transición), la FSM avanza. Verás tres ondas cuadradas rotando en secuencia: `red` se pone en alto por 8 ticks, luego `green` por 8 ticks, luego `yellow`, y de vuelta a `red`.

## De la simulación a un crucero real

`HOLD = 7` es una comodidad para la simulación. En una FPGA de 50 MHz, un tick son 20 ns, así que siete ticks son 140 ns — eso no es un semáforo, es un estrobo. Para una permanencia real de 1 segundo tendrías que contar hasta 50 000 000, lo que significa ampliar `count` y la constante `HOLD`. La forma limpia es un **generic**:

```vhdl
generic ( HOLD : natural := 7 );
...
signal count : unsigned(31 downto 0);
```

Simulas con un `HOLD` pequeño, sintetizas con uno grande — el mismo RTL, sin ediciones. Los cruceros reales también agregan un **estado de todo-en-rojo de seguridad** entre verdes en conflicto; agregar uno es un buen ejercicio para extender una máquina Moore sin romper su garantía de ausencia de glitches.

## Te toca

- **Agrega una entrada de solicitud peatonal.** Cuando `ped = '1'` y el estado sea `S_GREEN`, fuerza el siguiente estado a `S_YELLOW` (para que el ciclo se acorte y deje cruzar a los peatones). Esto convierte la máquina Moore en algo tipo Mealy — el siguiente estado ahora depende tanto de `state` **como** de la entrada `ped`, así que agrega `ped` a la lista de sensibilidad y un valor por defecto.
- **Emite una señal `walk`** que esté en alto solo cuando `state = S_RED`. Esa es una nueva salida Moore.
- **Parametriza `HOLD`** como un generic — 7 está bien para simulación, pero en una FPGA de 50 MHz querrías 50 millones de ciclos para lograr una permanencia de 1 s.

## Relación con la asignatura

Este ejemplo es la *aplicaciones con autómatas y lenguaje de descripción de hardware* de la Unidad II en miniatura: un autómata Moore determinista, dibujado como un diagrama de tres estados y luego descrito en HDL. El contador que estira cada estado hasta una permanencia temporizada también es tu primer sabor de la Unidad III (contadores) viviendo dentro de una máquina de estados.

## Qué recordar

- El semáforo es una FSM Moore de libro de texto: las salidas dependen solo de `state`, así que son libres de glitches.
- La división en dos procesos (registro con reloj + siguiente estado combinacional) mantiene el diseño legible y libre de latches.
- Un contador dentro del registro de estado convierte una transición en una permanencia temporizada; parametriza la permanencia con un generic para que un solo RTL sirva tanto para simulación como para silicio.
- En el momento en que una entrada influya en el siguiente estado, agrégala a la lista de sensibilidad y asígnale un valor por defecto a `next_state`.
