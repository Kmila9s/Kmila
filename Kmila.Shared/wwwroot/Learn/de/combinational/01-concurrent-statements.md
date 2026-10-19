# Nebenläufige Zuweisungen: `when/else` und `with/select`

Kombinatorische Logik in VHDL wird mit nebenläufigen Anweisungen geschrieben — jede Zeile außerhalb eines Prozesses beschreibt ein Stück fest verdrahtete Hardware. Zwei Syntax-Varianten existieren für denselben Job: **bedingte** (`when/else`) und **selektive** (`with/select`) Zuweisung. Das sind die „sentencias concurrentes“ aus der HDL-Einheit, und sie sind das Rückgrat jeder kombinatorischen Schaltung im Kurs: Addierer, Codewandler, Komparatoren, Multiplexer, Encoder, Decoder.

## `when/else` — bedingt

Gut, wenn du eine priorisierte Liste von Bedingungen hast oder wenn jeder Zweig ein *anderes* Signal prüft:

```vhdl
y <= (others => '0') when reset = '1' else
     a               when sel = "00"  else
     b               when sel = "01"  else
     c               when sel = "10"  else
     d;
```

Die erste passende Bedingung gewinnt. Die Reihenfolge zählt — es kompiliert zu einem **Prioritäts-MUX**: die oberste Bedingung hat den direktesten Pfad zu `y`. Beachte, dass alle Wert-Ausdrücke (`a`, `b`, `c`, `d`, das Null-Aggregat) **denselben Typ und dieselbe Breite** wie `y` haben müssen, und das finale `else` hat keine Bedingung — es ist der Auffangfall.

## `with/select` — selektiv

Gut, wenn du über einen einzelnen Ausdruck schaltest (so wie es ein Software-`switch`/`case` tut):

```vhdl
with sel select
  y <= a   when "00",
       b   when "01",
       c   when "10",
       'Z' when others;
```

Alle Fälle vergleichen gegen denselben Ausdruck (`sel`), die Zweige sind durch **Kommas** getrennt (nicht `else`), und `when others` ist **Pflicht**, um jeden möglichen Wert abzudecken. Dieser Stil kompiliert zu einem **balancierten MUX** — kein Zweig ist bevorzugt.

### Warum `when others` nicht verhandelbar ist

`sel` ist hier ein `std_logic_vector(1 downto 0)`. Du würdest denken, das sind vier Fälle — `"00"`, `"01"`, `"10"`, `"11"`. Aber jedes Bit ist ein *neunwertiges* `std_logic`, also zählt der Compiler 9² = 81 mögliche Muster (`"0X"`, `"ZU"`, …). Du kannst sie nicht aufzählen, also ist `when others` nötig, um die Anweisung vollständig zu machen. In der Synthese fallen die Metawerte weg und du bekommst den sauberen 4-fach-MUX, den du gemeint hast.

Du kannst Fälle auch mit dem Balken-Operator gruppieren:

```vhdl
with opcode select
  result <= a + b    when "000" | "001",   -- zwei Codes, derselbe Ausgang
            a - b    when "010",
            (others => '0') when others;
```

## Welche solltest du wählen?

- **Gleiches Signal, viele Muster** (Decoder, MUX, Codewandler): `with/select` liest sich wie eine Wahrheitstabelle. Nimm das.
- **Verschiedene Signale, Prioritätslogik** (Reset schlägt Enable schlägt Daten): `when/else`. Natürliche Wahl.
- **Syntheseausgabe:** auf einem modernen FPGA produzieren beide ungefähr dieselben Gatter, also ist die Wahl der klareren Form auf Quellcode-Ebene fast immer richtig. Greif nur dann zu `when/else`, wenn du wirklich Priorität willst; sonst signalisiert `with/select` dem nächsten Leser „keine Priorität“.

## Die Latch-Falle (Vorschau)

Beide Formen verlangen, dass `y` in **jedem** Szenario zugewiesen wird. Ein nebenläufiges `when/else` oder `with/select` kann `y` gar nicht unzugewiesen lassen — die Syntax erzwingt ein finales `else` / `when others`. Aber das äquivalente **if/case innerhalb eines Prozesses** kann es, und *dort* leitet der Synthesizer einen Latch ab, um den letzten Wert zu „merken“, was fast nie gewünscht ist. Es ist dieselbe Latch-Falle, der du in der Fix-it-Übung des sequentiellen Moduls frontal begegnest, also behalte die Angewohnheit: Weise den Ausgang auf jedem Pfad zu.

## Ein schneller durchgerechneter Anknüpfer: ein 2-Bit-Komparator

Der Betragskomparator aus dem Kurs fällt direkt aus `when/else` heraus:

```vhdl
-- gt/eq/lt sind einzelne std_logic-Ausgänge
gt <= '1' when unsigned(a) > unsigned(b) else '0';
eq <= '1' when a = b else '0';
lt <= '1' when unsigned(a) < unsigned(b) else '0';
```

Drei unabhängige nebenläufige Anweisungen, drei Komparatoren, alle gleichzeitig aktiv.

## Wo das als nächstes auftaucht

Du nutzt `with/select` im 3-zu-8-Decoder-Sample direkt nach dieser Lektion, und `when/else` durchgehend bei der Addierer-, Komparator- und MUX-Arbeit. Beides ist VHDL-Brot-und-Butter.

## Was du behalten solltest

- `when/else` = bedingt, priorisiert, verschiedene Signale erlaubt → Prioritäts-MUX.
- `with/select` = ein Selektor, komma-getrennte Fälle, `when others` Pflicht → balancierter MUX.
- Jeder Wert-Zweig muss zu Typ und Breite des Ziels passen.
- Nebenläufige Formen können den Ausgang nicht weglassen, aber die prozessbasierten `if`/`case`-Versionen schon — und genau dort schleichen sich Latches ein.
