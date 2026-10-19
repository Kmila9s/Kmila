# Hardware ist parallel

Studierende, die aus C, Python oder JavaScript kommen, haben alle dasselbe mentale Modell verinnerlicht: „Code läuft von oben nach unten, eine Anweisung nach der anderen.“ VHDL wird dich für diese Annahme bestrafen, bis du das richtige mentale Modell aufgebaut hast. Das ist der größte konzeptionelle Sprung in der ganzen HDL-Einheit, also lohnt es sich, hier langsamer zu machen.

## Der große Umschalter

Schau dir diese Architektur an:

```vhdl
architecture rtl of demo is
begin
  y1 <= a and b;
  y2 <= a or  b;
  y3 <= y1 xor y2;
end architecture rtl;
```

In einer Software-Sprache liest du das als drei sequentielle Schritte. In VHDL beschreiben alle drei Zeilen **Drähte, die gleichzeitig existieren**. Sie laufen nicht in Reihenfolge ab — sie existieren.

Der Synthesizer baut:

```mermaid
flowchart LR
    A([a]) --> AND["AND"]
    B([b]) --> AND
    A --> OR["OR"]
    B --> OR
    AND -- y1 --> XOR["XOR"]
    OR  -- y2 --> XOR
    XOR --> Y3([y3])
```

Alle drei Gatter sind gleichzeitig unter Strom. Wenn sich `a` oder `b` ändert, pegelt sich `y3` nach einer kleinen Laufzeitverzögerung über den jeweils längsten Pfad auf seinen neuen Wert ein. Es gibt kein „erstes“ und „zweites“.

## Warum die Reihenfolge egal ist

Du könntest die drei Zeilen in beliebiger Reihenfolge schreiben:

```vhdl
y3 <= y1 xor y2;   -- „Referenz vor der Deklaration“?
y1 <= a and b;
y2 <= a or  b;
```

Das ist immer noch gültiges VHDL und erzeugt die identische Schaltung. Die drei Zuweisungen beschreiben drei Drähte, und Drähten ist es egal, in welcher Reihenfolge du den Schaltplan gezeichnet hast. Das Tool liest alle drei und verkabelt sie.

## Die ereignisgesteuerte Engine dahinter

Wie simuliert also ein *sequentieller* Computer all das „auf einmal“? Jede nebenläufige Zuweisung verhält sich wie ein winziger Prozess mit einer impliziten Sensitivitätsliste: Sie wird **neu ausgewertet, wann immer sich ein Signal auf ihrer rechten Seite ändert**. Der Simulator führt eine Warteschlange anstehender Events; wenn sich `a` ändert, weckt er jede Zuweisung, die `a` liest, diese rechnen neu und planen vielleicht neue Events ein, und so weiter, bis die Schaltung ruhig ist. Dieses „Einpegeln“ ist der Grund, warum Nebenläufigkeit deterministisch statt chaotisch ist — ein Punkt, den die nächste Lektion (Delta-Zyklen) präzise macht.

## Wo „sequenziell“ wieder reinrutscht

Innerhalb eines `process` ist die Ausführung **sehr wohl** sequenziell — in diesem Prozess laufen die Anweisungen eine nach der anderen, von oben nach unten. Aber ein Prozess *als Ganzes* ist trotzdem nur eine weitere nebenläufige Einheit neben all den anderen.

```vhdl
architecture rtl of demo is
begin

  -- Nebenläufig 1
  y1 <= a and b;

  -- Nebenläufig 2 (ein Prozess, der intern sequenziell ist)
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

Alle drei nebenläufigen Einheiten leben gleichzeitig. Jede läuft, wann immer ihre Eingänge sich ändern. Innerhalb des Prozesses wird der Body der Reihe nach gelesen, aber das ist eine lokale Eigenschaft des Prozesses, nicht der Architektur. Tatsächlich ist eine nebenläufige Signalzuweisung *genau* die Kurzschreibweise für einen einzeiligen Prozess — die beiden sind austauschbar.

## Eine konkrete Folge: kombinatorische Schleifen

Dieses Design hat einen subtilen Bug:

```vhdl
architecture wrong of demo is
  signal a, b : std_logic;
begin
  a <= not b;
  b <= not a;
end architecture wrong;
```

Als Software gelesen sieht das aus wie „setze `a` auf not-b, dann setze `b` auf not-a“ — kein Problem. Als Hardware gelesen sind das zwei Inverter, in einer Schleife verdrahtet, ohne ein Register, das sie unterbricht. Das ist eine **kombinatorische Schleife**: ein Ring von Rückkopplung, der nie zur Ruhe kommt. Je nach Laufzeiten oszilliert er entweder oder rastet in einen unvorhersehbaren Zustand ein. Die Simulation dreht durch Delta-Zyklen (und meldet vielleicht einen Iterationslimit-Fehler), Synthese-Tools warnen vor einer kombinatorischen Schleife, und das Board glitcht.

> Die Lösung ist immer dieselbe: Unterbrich die Rückkopplung mit einem Register. Wenn du *Speicher willst*, führe die Schleife durch einen getakteten Prozess (so wird ein Flip-Flop gebaut). Wenn nicht, hast du einen Designfehler.

## Zähl die Hardware

Eine nützliche Angewohnheit, während du dein Gehirn umverdrahtest: Lies eine Architektur und zähl die Gatter. Das erste Beispiel sind 3 Gatter (AND, OR, XOR). Dieses hier —

```vhdl
y <= (a and b) or (c and d) or (e and f);
```

— sind drei AND-Gatter, die ein 3-Eingang-OR speisen, alle gleichzeitig aktiv. Du zählst nicht länger *Schritte*; du zählst *Bauteile*.

## Die Umverdrahtung in deinem Kopf

- Hör auf zu denken „was läuft zuerst?“
- Fang an zu denken „welche Drähte existieren, und was speist was?“
- Innerhalb eines Prozesses ist das alte „eines nach dem anderen“-Modell wieder zurück — aber nur in diesem Prozess.
- Schließe nie einen kombinatorischen Pfad ohne Register auf sich selbst.

Sobald das klickt, hört VHDL auf, gegen dich zu kämpfen. Du liest eine Architektur und siehst sofort den Schaltplan, den sie repräsentiert, statt der „Programm-Ausführung“, die du dir früher vorgestellt hast.

## Was du behalten solltest

- Nebenläufige Anweisungen beschreiben Drähte, die alle gleichzeitig existieren; ihre Reihenfolge in der Datei ist irrelevant.
- Der Simulator täuscht das mit einer Event-Warteschlange vor — Zuweisungen laufen neu, wann immer ein Eingang sich ändert.
- Ein `process` ist eine nebenläufige Einheit; nur *in ihm* ist Code sequenziell.
- Eine kombinatorische Rückkopplungsschleife ohne Register ist ein Bug — unterbrich sie mit einem getakteten Prozess.
