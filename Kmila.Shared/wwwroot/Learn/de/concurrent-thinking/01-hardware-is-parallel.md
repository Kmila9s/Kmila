# Hardware ist parallel

Studierende, die von C, Python oder JavaScript kommen, haben alle dasselbe Denkmodell verinnerlicht: "Code läuft von oben nach unten, eine Anweisung nach der anderen." VHDL wird dich für diese Annahme bestrafen, bis du das richtige Denkmodell aufbaust.

## Der große Paradigmenwechsel

Sieh dir diese Architektur an:

```vhdl
architecture rtl of demo is
begin
  y1 <= a and b;
  y2 <= a or  b;
  y3 <= y1 xor y2;
end architecture rtl;
```

In einer Software-Sprache würdest du das als drei sequentielle Schritte lesen. In VHDL beschreiben alle drei Zeilen **Drähte, die gleichzeitig existieren**. Sie werden nicht in Reihenfolge ausgeführt — sie existieren.

Der Synthesizer baut:

```
   a ─┬─ AND ─── y1 ──┐
      │               XOR ── y3
   b ─┴─ OR  ── y2 ───┘
```

Alle drei Gatter sind gleichzeitig aktiv. Wenn sich `a` oder `b` ändert, stabilisiert sich der Ausgang `y3` auf seinem neuen Wert nach einer kleinen Laufzeitverzögerung durch den längsten Pfad. Es gibt kein "zuerst" und "danach".

## Warum die Reihenfolge keine Rolle spielt

Du könntest die drei Zeilen in beliebiger Reihenfolge schreiben:

```vhdl
y3 <= y1 xor y2;   -- Referenz vor "Deklaration"?
y1 <= a and b;
y2 <= a or  b;
```

Das ist immer noch gültiges VHDL. Die drei Zuweisungen beschreiben drei Drähte, und Drähten ist es egal, in welcher Reihenfolge du das Schaltbild gezeichnet hast. Das Tool liest alle drei und verdrahtet sie.

## Wo "sequentiell" zurückschleicht

Innerhalb eines `process` sind die Dinge **tatsächlich** sequentiell — innerhalb dieses Prozesses eine Anweisung nach der anderen, von oben nach unten. Aber ein Prozess als Ganzes ist weiterhin nur eine weitere nebenläufige Einheit neben allen anderen.

```vhdl
architecture rtl of demo is
begin

  -- Nebenläufig 1
  y1 <= a and b;

  -- Nebenläufig 2 (ein Prozess, der intern sequentiell ist)
  process(clk)
  begin
    if rising_edge(clk) then
      counter <= counter + 1;
    end if;
  end process;

  -- Nebenläufig 3
  y2 <= a or b;

end architecture rtl;
```

Alle drei nebenläufigen Einheiten sind gleichzeitig aktiv. Jede läuft, wenn sich ihre Eingänge ändern. Innerhalb des Prozesses wird der Rumpf in Reihenfolge gelesen, aber das ist eine lokale Eigenschaft des Prozesses, nicht der Architektur.

## Eine konkrete Konsequenz

Dieses Design hat einen subtilen Wettlauf:

```vhdl
architecture wrong of demo is
  signal a, b : std_logic;
begin
  a <= not b;
  b <= not a;
end architecture wrong;
```

Als Software gelesen sieht das aus wie "setze `a` auf not-b, dann setze `b` auf not-a" — kein Problem. Als Hardware gelesen sind es zwei Inverter, die zu einer Schleife verschaltet sind. Das ist ein **Oszillator** — ein Rückkopplungsring, der nie stabil werden kann. Die Simulation wird sich beschweren, die Synthese wird ihn (manchmal) wegoptimieren, und das Board wird unvorhersehbar glitchen.

## Die Neu-Verdrahtung in deinem Kopf

- Höre auf zu denken "was läuft zuerst?"
- Fang an zu denken "welche Drähte existieren?"
- Innerhalb eines Prozesses ist das alte "eins-nach-dem-anderen"-Modell zurück — aber nur innerhalb dieses Prozesses.

Sobald das klickt, hört VHDL auf, gegen dich zu kämpfen. Du wirst eine Architektur lesen und sofort das Schaltbild sehen, das sie repräsentiert — nicht die "Programmausführung", die du dir früher vorgestellt hast.
