# Beispiel: Ampel-FSM

Eine Ampel ist die kanonische Moore-FSM: drei Zustände (`RED`, `GREEN`, `YELLOW`), ein Ausgang pro Farbe und ein Zähler, der sagt "halte jeden Zustand für ein paar Zyklen, bevor du weitergehst."

## Die Struktur

Das Beispiel folgt dem **Zwei-Prozess-Muster**:

- Prozess 1 ist das **Zustandsregister** — ein klassisches D-Flipflop, das `state` speichert und einen Haltezähler tickt.
- Prozess 2 ist die **Folgezustandslogik** — rein kombinatorisch, entscheidet, was `next_state` bei gegebenem aktuellem Zustand ist.
- Die Ausgangszuweisungen (unten in der Architektur) sind kombinatorische Moore-Ausgänge — ihre Werte hängen nur von `state` ab.

## Durch den Code gehen

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 Ticks pro Farbe
```

`state_t` ist eine **Aufzählung** — der Synthesizer wählt die tatsächliche Binärkodierung, wir verweisen nur auf benannte Zustände. `count` verfolgt, wie lange wir im aktuellen Zustand waren. `HOLD` ist die Verweildauer.

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

Ausgänge (reines Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## Was das Beispiel macht

Der Takt läuft etwa 400 µs. Alle 8 Takte (7 Halten + 1 Übergang) schreitet die FSM voran. Du wirst drei Rechteckwellen in Folge rotieren sehen: `red` geht für 8 Ticks hoch, dann `green` für 8 Ticks, dann `yellow`, dann zurück zu `red`.

## Du bist dran

- **Füge einen Fußgänger-Anforderungseingang hinzu.** Wenn `ped = '1'` und der Zustand `S_GREEN` ist, erzwinge den nächsten Zustand auf `S_YELLOW` (damit sich der Zyklus verkürzt, um Fußgänger übergehen zu lassen). Das macht aus der Moore-Maschine etwas Mealy-ähnliches — der Ausgang `yellow` hängt sowohl von `state` **als auch** vom `ped`-Eingang ab.
- **Erzeuge ein `walk`-Signal,** das nur dann hoch ist, wenn `state = S_RED`. Das ist ein neuer Moore-Ausgang.
- **Parametriere `HOLD`** als Generic — 7 ist fein für die Simulation, aber auf einem 50-MHz-FPGA möchtest du ein paar hundert Millionen Zyklen, um eine 1-s-Haltezeit zu bekommen.
