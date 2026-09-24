# Datentypen: `std_logic`, Vektoren, `unsigned`, `integer`

VHDL ist eine typisierte Sprache. Wenn du einen Port als `in std_logic` deklariert siehst, lässt der Compiler nicht zu, dass du ihn an etwas mit einem anderen Typ verkabelst. Den richtigen Typ für jedes Signal zu wählen ist die halbe Miete für sauberes VHDL.

## `std_logic` — der universelle Draht

Ein einzelnes Bit der **9-wertigen** IEEE-Logik. Ja, neun:

| Wert  | Bedeutung |
|-------|-----------|
| `'0'` | starkes Low |
| `'1'` | starkes High |
| `'Z'` | hochohmig (Tristate aus) |
| `'L'` | schwaches Low (Pull-down) |
| `'H'` | schwaches High (Pull-up) |
| `'X'` | unbekannt / mehrere Treiber widersprechen sich |
| `'U'` | nicht initialisiert — dein Signal wurde nie zugewiesen |
| `'W'` | schwaches unbekannt |
| `'-'` | don't care (in case-Anweisungen) |

In der Synthese schaffen es nur `'0'`, `'1'` und `'Z'` in die Hardware. Die anderen sind Simulationsdiagnosen — siehst du `'U'` in einer Wellenform, hast du ein Signal, das gelesen wird, bevor es geschrieben ist.

## `std_logic_vector` — ein Bus

Ein Array fester Breite aus `std_logic`:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 Bit, Indexes 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 Bit, Indexes 0..15
```

`downto` ist mit Abstand die häufigere Richtung — passt zu der Art, wie wir Zahlen schreiben (MSB links).

Literale:

```vhdl
data <= "10101010";     -- Binärliteral, 1 Zeichen pro Bit
data <= x"AA";          -- Hex-Literal (Vielfaches von 4 Bit)
data <= (others => '0');-- Kurzschreibweise für nur-Nullen
data <= (0 => '1', others => '0'); -- nur LSB high
```

## `unsigned` / `signed` — Vektoren, auf denen du rechnen kannst

`std_logic_vector` ist „nur ein Sack Bits". Hat keine numerische Bedeutung. Zum Addieren oder Subtrahieren brauchst du `unsigned` oder `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- funktioniert
```

Wechseln zwischen ihnen:

```vhdl
-- Vektor → unsigned für Mathe
u <= unsigned(slv);

-- unsigned → Vektor für die Außenwelt
slv <= std_logic_vector(u);

-- unsigned → integer für Indizierung oder Konstanten
idx <= to_integer(u);

-- integer → unsigned (Breite angeben)
u <= to_unsigned(42, 8);
```

Faustregel: nutze `std_logic_vector` in **Ports** (das breiteste Publikum) und konvertiere innerhalb der Architecture zu `unsigned` / `signed`, wo du tatsächlich rechnest.

## `integer` — für Indizierung und Zählung

Nutze `integer` (oder einen eingeschränkten Subtyp) für Schleifenzähler, Konstanten und alles, was keine Port-Grenze überquert:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Unbeschränkter `integer` ist in der Simulation normalerweise 32 Bit. Für Hardware willst du einen eingeschränkten Bereich:

```vhdl
signal counter : integer range 0 to 255;
```

Der Synthesizer gibt dir dann genau 8 Bit.

## Aufzählungen — für FSM-Zustände

Deklariere einen lesbaren Typ für Zustandsmaschinen:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

Du schreibst dann `state <= S_ACTIVE` statt `state <= "010"`. Der Synthesizer wählt die Binärcodierung für dich (du kannst mit einem Attribut hinten anregen, wenn dir das wichtig ist).

## Was du behalten solltest

- Einzelner Draht → `std_logic`. Bus → `std_logic_vector`.
- Musst du rechnen? Nimm `unsigned` (oder `signed`) und `IEEE.NUMERIC_STD`.
- **Ports** in `std_logic_vector` zu deklarieren, macht deine Entity maximal portabel.
- Für FSM-Zustände nimm Aufzählungen — dein Code liest sich wie eine Spec.
