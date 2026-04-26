# Datentypen: `std_logic`, Vektoren, `unsigned`, `integer`

VHDL ist eine typisierte Sprache. Wenn du einen Port als `in std_logic` deklariert siehst, lässt dich der Compiler ihn nicht an etwas eines anderen Typs verdrahten. Den richtigen Typ für jedes Signal zu wählen ist die halbe Miete, um sauberes VHDL zu schreiben.

## `std_logic` — der universelle Draht

Ein einzelnes Bit der **9-wertigen** IEEE-Logik. Ja, neun:

| Wert  | Bedeutung |
|-------|-----------|
| `'0'` | starkes Low-Driving |
| `'1'` | starkes High-Driving |
| `'Z'` | hochohmig (Tristate aus) |
| `'L'` | schwaches Low (Pull-down) |
| `'H'` | schwaches High (Pull-up) |
| `'X'` | unbekannt / mehrere widersprüchliche Treiber |
| `'U'` | uninitialisiert — dein Signal wurde nie zugewiesen |
| `'W'` | schwaches unbekannt |
| `'-'` | don't care (in case-Anweisungen verwendet) |

In der Synthese schaffen es nur `'0'`, `'1'` und `'Z'` in die Hardware. Die anderen sind Simulations-Diagnosen — wenn du `'U'` in einer Waveform siehst, hast du ein Signal, das gelesen wird, bevor es geschrieben ist.

## `std_logic_vector` — ein Bus

Ein Array fester Breite aus `std_logic`:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 Bits, Indizes 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 Bits, Indizes 0..15
```

`downto` ist bei weitem die häufigere Richtung — sie entspricht der Art, wie wir Zahlen schreiben (MSB links).

Literale:

```vhdl
data <= "10101010";     -- Binärliteral, ein Zeichen pro Bit
addr <= x"CAFE";        -- Hex-Literal (muss ein Vielfaches von 4 Bits sein)
data <= (others => '0');-- Kurzform für alles Nullen
data <= (0 => '1', others => '0'); -- nur das LSB hoch
```

## `unsigned` / `signed` — Vektoren, mit denen du rechnen kannst

`std_logic_vector` ist "nur eine Tüte Bits". Es hat keine numerische Bedeutung. Um zu addieren oder zu subtrahieren, brauchst du `unsigned` oder `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- funktioniert
```

Dazwischen wechseln:

```vhdl
-- Vektor → unsigned fürs Rechnen
u <= unsigned(slv);

-- unsigned → Vektor für die Außenwelt
slv <= std_logic_vector(u);

-- unsigned → integer für Indizierung oder Konstanten
idx <= to_integer(u);

-- integer → unsigned (Breite angeben)
u <= to_unsigned(42, 8);
```

Faustregel: Verwende `std_logic_vector` in **Ports** (das breiteste Publikum) und konvertiere innerhalb von Architectures zu `unsigned` / `signed`, wo du tatsächlich Arithmetik betreibst.

## `integer` — zum Indizieren und Zählen

Verwende `integer` (oder einen eingeschränkten Subtyp) für Schleifenzähler, Konstanten und alles, was keine Portgrenze überschreitet:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Unbeschränkter `integer` ist in der Simulation meist 32-Bit. Für Hardware willst du einen eingeschränkten Bereich:

```vhdl
signal counter : integer range 0 to 255;
```

Der Synthesizer gibt dir dann genau 8 Bits.

## Aufzählungen — für FSM-Zustände

Deklariere einen lesbaren Typ für Zustandsautomaten:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

Du schreibst dann `state <= S_ACTIVE` statt `state <= "010"`. Der Synthesizer wählt die Binärkodierung für dich (und du kannst mit einem Attribut einen Hinweis geben, wenn es dir wichtig ist).

## Was zu merken ist

- Einzelner Draht → `std_logic`. Bus → `std_logic_vector`.
- Muss rechnen? Verwende `unsigned` (oder `signed`) und `IEEE.NUMERIC_STD`.
- Ports in `std_logic_vector` zu deklarieren, macht deine Entity maximal portabel.
- Für FSM-Zustände verwende Aufzählungen — dein Code liest sich wie eine Spezifikation.
