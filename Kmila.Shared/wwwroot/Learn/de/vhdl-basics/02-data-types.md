# Datentypen: `std_logic`, Vektoren, `unsigned`, `integer`

VHDL ist eine **streng typisierte** Sprache. Wenn du einen Port als `in std_logic` deklariert siehst, lässt dich der Compiler ihn nicht ohne eine explizite Konvertierung mit etwas anderem Typs verdrahten. Für jedes Signal den richtigen Typ zu wählen ist die halbe Miete, wenn man sauberes VHDL schreiben will.

## `std_logic` — der universelle Draht

Ein einzelnes Bit der **9-wertigen** IEEE-1164-Logik. Ja, neun:

| Wert | Bedeutung |
|-------|---------|
| `'0'` | stark getrieben Low |
| `'1'` | stark getrieben High |
| `'Z'` | hochohmig (Tristate aus) |
| `'L'` | schwach Low (Pull-down) |
| `'H'` | schwach High (Pull-up) |
| `'X'` | stark unbekannt — z. B. zwei Treiber, die sich mit verschiedenen Werten streiten |
| `'U'` | uninitialisiert — deinem Signal wurde nie etwas zugewiesen |
| `'W'` | schwach unbekannt |
| `'-'` | don't care (als `std_match`-Muster / Synthese-Hinweis genutzt) |

Warum neun statt zwei? Weil ein echter Draht nicht nur „0 oder 1“ ist. Er kann floaten (`'Z'`), schwach gezogen sein (`'L'`/`'H'`) oder in echtem Konflikt stehen (`'X'`). Das ist es, was `std_logic` zu einem **aufgelösten** (resolved) Typ macht: Wenn mehrere Treiber sich einen Draht teilen, entscheidet eine Auflösungsfunktion über das Ergebnis. Ein starker Wert schlägt einen schwachen; zwei starke Werte, die sich widersprechen, ergeben `'X'`; und ein nicht getriebener Draht sitzt bei `'Z'`.

In der Synthese landen nur `'0'`, `'1'` und `'Z'` in der Hardware. Die übrigen sind Simulations-Diagnosen — wenn du `'U'` in einem Waveform siehst, hast du ein Signal, das gelesen wird, bevor es geschrieben wurde; ein Streifen `'X'` bedeutet meist einen Bus mit zwei aktiven Treibern.

> Es gibt auch einen nicht aufgelösten Verwandten, `std_ulogic` (das `u` steht für „unresolved“). `std_logic` ist buchstäblich `std_ulogic` plus eine Auflösungsfunktion. Nutze standardmäßig `std_logic` — die Auflösung ist genau das, was du für Busse willst.

## `std_logic_vector` — ein Bus

Ein Array fester Breite aus `std_logic`:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 Bits, Indizes 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 Bits, Indizes 0..15
```

`downto` ist mit Abstand die häufigere Richtung — sie passt zu der Art, wie wir Zahlen schreiben (MSB links, also ist `data(7)` das höchstwertige Bit). Wähle pro Projekt eine Richtung und bleib dabei; `to` und `downto` zu mischen ist eine klassische Quelle für stillschweigend vertauschte Busse.

Literale:

```vhdl
data <= "10101010";      -- Binärliteral, ein Zeichen pro Bit
addr <= x"CAFE";         -- Hexliteral (Breite muss ein Vielfaches von 4 Bit sein)
data <= (others => '0'); -- Aggregat: alles Null, beliebige Breite
data <= (0 => '1', others => '0'); -- nur Bit 0 (das LSB) High
```

Die Form `(others => '0')` sollte man lieben: Sie passt sich automatisch an, wenn du später die Breite änderst, und wird so nie veraltet.

## `unsigned` / `signed` — Vektoren, mit denen du rechnen kannst

`std_logic_vector` ist „nur ein Sack voller Bits“. Es hat keine numerische Bedeutung, also ist `slv1 + slv2` mit `numeric_std` ein Compile-Fehler. Zum Addieren oder Subtrahieren brauchst du `unsigned` oder `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- funktioniert: Addition gleicher Breite, unsigned
```

Die beiden Typen unterscheiden sich nur darin, wie das oberste Bit gelesen wird: `unsigned` behandelt alle Bits als Betrag; `signed` behandelt das MSB als Vorzeichen im Zweierkomplement.

Zwischen ihnen wechseln — das sind die vier Konvertierungen, die du ständig brauchst:

```vhdl
-- dieselben Bits als unsigned neu interpretieren (zum Rechnen)
u   <= unsigned(slv);

-- ein unsigned als reine Bits neu interpretieren (für die Außenwelt / einen Port)
slv <= std_logic_vector(u);

-- unsigned -> integer, zum Indizieren oder für Konstanten
idx <= to_integer(u);

-- integer -> unsigned; du MUSST die Breite angeben
u   <= to_unsigned(42, 8);
```

`unsigned(slv)` und `std_logic_vector(u)` sind **Type-Casts** — sie ändern das Etikett auf den Bits, nicht die Bits. `to_integer` und `to_unsigned` sind **Funktionen**, die tatsächlich rechnen. Ein häufiger Anfängerfehler ist, `to_unsigned` auf einen Vektor anzuwenden (es nimmt einen Integer) oder einen Integer mit `unsigned(…)` zu casten (Casts funktionieren nur zwischen den Array-Typen).

Noch einer, der dir Overflow-Bugs erspart — `resize`, das auf eine neue Breite auffüllt oder abschneidet. Es **erweitert `unsigned` mit Nullen** und **`signed` mit dem Vorzeichen**, erhält also den Wert vor der Arithmetik:

```vhdl
wide <= resize(a, 9) + resize(b, 9);  -- 9-Bit-Ergebnis behält den Übertrag
```

Faustregel: Nutze `std_logic_vector` in **Ports** (das breiteste Publikum) und konvertiere zu `unsigned` / `signed` innerhalb der Architectures, wo du tatsächlich rechnest.

## `integer` — zum Indizieren und Zählen

Nutze `integer` (oder einen eingeschränkten Subtyp) für Schleifenzähler, Konstanten und alles, was keine Portgrenze überschreitet:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Unbeschränkter `integer` ist in der Simulation mindestens 32-bit (Bereich −2³¹+1 bis 2³¹−1). Für Hardware willst du fast immer einen eingeschränkten Bereich, damit der Synthesizer das Register passend dimensioniert:

```vhdl
signal counter : integer range 0 to 255;  -- synthetisiert zu genau 8 Bit
```

Ein unbeschränkter `integer`-Zähler kostet dich 32 Flip-Flops, die du nicht wolltest.

## Aufzählungen — für FSM-Zustände

Deklariere einen lesbaren Typ für Zustandsmaschinen:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

Dann schreibst du `state <= S_ACTIVE` statt `state <= "010"`. Der Synthesizer wählt die Binär- (oder One-Hot-)Codierung für dich, und du kannst ihn mit einem Attribut lenken, wenn es dir wichtig ist. Dein Code liest sich wie eine Spezifikation, und das Waveform zeigt die Zustands*namen*, nicht rohe Bits.

## Was du behalten solltest

- Einzelner Draht → `std_logic`. Bus → `std_logic_vector`. Beide kommen aus `STD_LOGIC_1164`.
- Musst du rechnen? Nutze `unsigned` (oder `signed`) und `IEEE.NUMERIC_STD`. `std_logic_vector` hat keine numerische Bedeutung.
- `unsigned(x)` / `std_logic_vector(x)` sind Casts (Bits umetikettieren); `to_integer` / `to_unsigned` sind Funktionen (und `to_unsigned` braucht eine Breite).
- **Ports** in `std_logic_vector` zu deklarieren macht deine Entity maximal portabel; konvertiere im Inneren.
- Schränke deine `integer`-Bereiche ein und nutze Aufzählungen für FSM-Zustände.
