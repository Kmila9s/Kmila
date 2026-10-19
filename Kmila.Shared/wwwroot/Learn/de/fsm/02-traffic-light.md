# Beispiel: Ampel-Zustandsautomat

Eine Ampel ist der kanonische Moore-Zustandsautomat: drei Zustände (`RED`, `GREEN`, `YELLOW`), ein Ausgang pro Farbe und ein Timer, der sagt „halte jeden Zustand ein paar Zyklen, bevor du weiterziehst".

## Die Struktur

Das Beispiel folgt dem **Zwei-Prozess-Muster**:

- Prozess 1 ist das **Zustandsregister** – ein klassisches D-Flip-Flop, das `state` speichert und einen Halte-Zähler taktet.
- Prozess 2 ist die **Folgezustandslogik** – rein kombinatorisch, entscheidet, was `next_state` gegeben dem aktuellen ist.
- Die Ausgangszuweisungen (unten in der Architektur) sind kombinatorische Moore-Ausgänge – ihre Werte hängen nur von `state` ab.

## Den Code durchgehen

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 Ticks pro Farbe
```

`state_t` ist eine **Aufzählung** – der Synthesizer wählt die tatsächliche Binärcodierung, wir verweisen nur auf benannte Zustände. `count` verfolgt, wie lange wir im aktuellen Zustand sind. `HOLD` ist die Verweildauer.

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

Zwei Details lohnen es, benannt zu werden. Erstens ist das ein **asynchroner Reset** (`reset` steht in der Sensitivitätsliste und wird vor `rising_edge` geprüft) – die Ampel springt in dem Moment auf `S_RED`, in dem der Reset aktiv wird. Ein synchroner Reset würde `reset` aus der Sensitivitätsliste nehmen und ihn nur im `rising_edge`-Zweig prüfen; beide sind gültig, wähle einfach einen Stil und bleib konsistent. Zweitens macht der Zähler aus einem Zustandsübergang eine 8-Zyklen-Verweildauer – der Zustand rückt nur in dem Zyklus vor, in dem `count = HOLD` gilt.

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

Weil dieser Prozess `next_state` auf jedem Zweig zuweist und das `case` über `state_t` erschöpfend ist, gibt es hier kein abgeleitetes Latch. Die Sensitivitätsliste ist nur `(state)` – korrekt, denn `next_state` hängt von nichts anderem ab. In dem Moment, in dem du den Folgezustand von einem Eingang abhängig machst, muss dieser Eingang in die Sensitivitätsliste (sonst simuliert der kombinatorische Prozess falsch), und du solltest als Absicherung ein `next_state <= state;` als Default in die erste Zeile setzen.

Ausgänge (rein Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

Jeder Ausgang ist eine Funktion von `state` allein – sie ändern sich also nur, wenn das Register taktet, und können nicht zwischen den Flanken glitchen. Genau das ist der ganze Reiz eines Moore-Automaten für etwas, das ein physisches Signal treibt.

## Was das Beispiel tut

Der Takt läuft ungefähr 400 µs. Alle 8 Takte (7 Halten + 1 Übergang) rückt der Automat vor. Du siehst drei Rechteckwellen, die nacheinander rotieren: `red` ist 8 Ticks high, dann `green` 8 Ticks, dann `yellow`, dann zurück zu `red`.

## Von der Simulation zur echten Kreuzung

`HOLD = 7` ist eine Bequemlichkeit für die Simulation. Auf einem 50-MHz-FPGA dauert ein Tick 20 ns, also sind sieben Ticks 140 ns – keine Ampel, ein Stroboskop. Für eine echte 1-Sekunde-Verweildauer würdest du bis 50 000 000 zählen, was `count` und die `HOLD`-Konstante verbreitert. Der saubere Weg ist ein **Generic**:

```vhdl
generic ( HOLD : natural := 7 );
...
signal count : unsigned(31 downto 0);
```

Simuliere mit einem winzigen `HOLD`, synthetisiere mit einem großen – dasselbe RTL, keine Änderungen. Echte Kreuzungen fügen außerdem einen **Alles-Rot-Sicherheitszustand** zwischen widersprüchlichen Grünphasen hinzu; einen hinzuzufügen ist eine gute Übung darin, einen Moore-Automaten zu erweitern, ohne seine Glitch-Freiheit zu brechen.

## Du bist dran

- **Füge einen Fußgängeranforderungs-Eingang hinzu.** Wenn `ped = '1'` und der Zustand `S_GREEN` ist, erzwinge `S_YELLOW` als Folgezustand (so wird der Zyklus verkürzt, damit Fußgänger queren können). Das macht aus dem Moore-Automaten etwas Mealy-Artiges – der Folgezustand hängt jetzt von `state` **und** dem `ped`-Eingang ab, also füge `ped` zur Sensitivitätsliste und eine Default-Zuweisung hinzu.
- **Gib ein `walk`-Signal aus**, das nur high ist, wenn `state = S_RED`. Das ist ein neuer Moore-Ausgang.
- **Parametrisiere `HOLD`** als Generic – 7 ist für die Simulation okay, aber auf einem 50-MHz-FPGA bräuchtest du 50 Millionen Zyklen für eine 1-s-Haltephase.

## Bezug zum Kurs

Dieses Beispiel ist Einheit IIs *aplicaciones con autómatas y lenguaje de descripción de hardware* im Kleinen: ein deterministischer Moore-Automat, gezeichnet als Drei-Zustands-Diagramm und dann in HDL beschrieben. Der Zähler, der jeden Zustand zu einer getimten Verweildauer streckt, ist auch dein erster Vorgeschmack auf Einheit III (contadores), der in einem Zustandsautomaten lebt.

## Was du behalten solltest

- Die Ampel ist ein Lehrbuch-Moore-Automat: Ausgänge hängen nur von `state` ab, also sind sie glitch-frei.
- Die Zwei-Prozess-Teilung (getaktetes Register + kombinatorischer Folgezustand) hält den Entwurf lesbar und latch-frei.
- Ein Zähler im Zustandsregister macht aus einem Übergang eine getimte Verweildauer; parametrisiere die Verweildauer mit einem Generic, damit ein RTL sowohl Simulation als auch Silizium bedient.
- In dem Moment, in dem ein Eingang den Folgezustand beeinflusst, füge ihn zur Sensitivitätsliste hinzu und weise `next_state` einen Default zu.
