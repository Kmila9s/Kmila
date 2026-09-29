# Beispiel: Ampel-FSM

Eine Ampel ist der kanonische Moore-FSM: drei Zustände (`RED`, `GREEN`, `YELLOW`), ein Ausgang pro Farbe und ein Timer, der sagt „halte jeden Zustand ein paar Zyklen, bevor du weiterziehst“.

## Die Struktur

Das Beispiel folgt dem **Zwei-Prozess-Muster**:

- Prozess 1 ist das **Zustandsregister** — ein klassisches D-Flip-Flop, das `state` speichert und einen Halte-Zähler taktet.
- Prozess 2 ist die **Folgezustandslogik** — rein kombinatorisch, entscheidet, was `next_state` gegeben dem aktuellen ist.
- Die Ausgangszuweisungen (unten in der Architektur) sind kombinatorische Moore-Ausgänge — ihre Werte hängen nur von `state` ab.

## Den Code durchgehen

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 Ticks pro Farbe
```

`state_t` ist eine **Aufzählung** — der Synthesizer wählt die tatsächliche Binärcodierung, wir verweisen nur auf benannte Zustände. `count` verfolgt, wie lange wir im aktuellen Zustand sind. `HOLD` ist die Verweildauer.

Zustandsregister:

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

Folgezustandslogik:

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

Ausgänge (rein Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## Was das Beispiel tut

Der Takt läuft ungefähr 400 µs. Alle 8 Takte (7 Halten + 1 Übergang) rückt der FSM vor. Du siehst drei Rechteckwellen, die nacheinander rotieren: `red` ist 8 Ticks high, dann `green` 8 Ticks, dann `yellow`, dann zurück zu `red`.

## Du bist dran

- **Füge einen Fußgängeranforderungs-Eingang hinzu.** Wenn `ped = '1'` und der Zustand `S_GREEN` ist, erzwinge `next_state <= S_YELLOW` (so wird der Zyklus verkürzt, damit Fußgänger queren können). Das macht aus dem Moore-Automaten etwas Mealy-Artiges — der Ausgang `yellow` hängt jetzt von `state` **und** `ped` ab.
- **Gib ein `walk`-Signal aus**, das nur high ist, wenn `state = S_RED`. Das ist ein neuer Moore-Ausgang.
- **Parametrisiere `HOLD`** als Generic — 7 reicht für die Sim, aber auf einem 50-MHz-FPGA brauchst du ein paar hundert Millionen Zyklen, um 1 s zu halten.
