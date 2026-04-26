# Ejemplo: FSM de semáforo

Un semáforo es el FSM Moore canónico: tres estados (`RED`, `GREEN`, `YELLOW`), una salida por color y un temporizador que dice "mantén cada estado unos ciclos antes de avanzar".

## La estructura

El ejemplo sigue el **patrón de dos procesos**:

- Proceso 1 es el **registro de estado** — un D flip-flop clásico que guarda `state` y corre un contador de permanencia.
- Proceso 2 es la **lógica de siguiente estado** — puramente combinacional, decide `next_state` dado el actual.
- Las asignaciones de salida (al final de la arquitectura) son salidas Moore combinacionales — dependen solo de `state`.

## Paso a paso

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 ticks por color
```

`state_t` es una **enumeración** — el sintetizador elige la codificación binaria, nosotros solo nombramos estados. `count` mide cuánto llevamos en el estado. `HOLD` es el tiempo de espera.

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

Siguiente estado:

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

Salidas (Moore puras):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## Qué hace el ejemplo

El reloj corre ~400 µs. Cada 8 ciclos (7 de espera + 1 de transición) la FSM avanza. Verás tres ondas cuadradas rotando: `red` alto 8 ticks, luego `green` 8, luego `yellow`, y vuelta a `red`.

## Tu turno

- **Agrega una solicitud peatonal.** Con `ped = '1'` y estado `S_GREEN`, fuerza `next_state` a `S_YELLOW` (el ciclo se acorta para dejar cruzar). Esto convierte la Moore en una Mealy — la salida `yellow` depende de `state` **y** de `ped`.
- **Emite `walk`** que sea alto solo cuando `state = S_RED`. Nueva salida Moore.
- **Parametriza `HOLD`** como generic — 7 está bien para simulación, pero en un FPGA de 50 MHz querrás cientos de millones de ciclos para un retardo de 1 s.
