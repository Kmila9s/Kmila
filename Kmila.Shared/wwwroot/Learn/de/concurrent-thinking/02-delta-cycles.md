# Delta-Zyklen und Race Conditions

Ein Simulator muss "zwei gleichzeitig geschehenden Dingen" eine deterministische Reihenfolge geben. VHDLs Antwort ist der **Delta-Zyklus**: eine unendlich kleine Zeitscheibe, die Ursache von Wirkung trennt.

## Was ein Delta-Zyklus ist

Die Simulationszeit schreitet in echten Schritten voran (gemessen in ns, µs, ms). Aber innerhalb eines einzelnen Zeitschritts können Änderungen in **Delta-Zyklus**-Reihenfolge passieren:

- δ₀: du planst ein Signal-Update.
- δ₁: das Update wird wirksam; jeder Prozess, der auf dieses Signal sensitiv ist, hat jetzt neue Eingaben.
- δ₂: diese Prozesse planen ihre Updates.
- δ₃: diese Updates werden wirksam.
- …

Jedes Delta ist "null reale Zeit", repräsentiert aber dennoch einen kausalen Schritt. Der Simulator durchläuft die Deltas, bis nichts mehr aktualisiert werden muss — erst dann schreitet die reale Zeit fort.

## Warum es das gibt

Betrachte zwei Signale, die beide an derselben Taktflanke aktualisiert werden:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

Ohne Delta-Zyklen müsste der Simulator eine willkürliche Reihenfolge wählen — wird `a` vor `b` aktualisiert? Danach? Genau zum selben Zeitpunkt? Delta-Zyklen umgehen das: Beide Zuweisungen werden bei δ₀ geplant, und beide werden bei δ₁ wirksam. Jeder Prozess, der `a` oder `b` liest, sieht beide neuen Werte gleichzeitig.

## Die Race-Condition-Falle

Sieh dir das an:

```vhdl
process(a)
begin
  b <= a;
end process;

process(b)
begin
  c <= b;
end process;
```

Wenn sich `a` ändert, löst der erste Prozess aus und plant `b`. Bei δ₁ hat `b` seinen neuen Wert. Der zweite Prozess löst aus, plant `c`. Bei δ₂ hat `c` den richtigen Wert.

Aber vertausche jetzt die beiden Prozesse. Ändert sich etwas? **Nein.** Delta-Zyklen garantieren, dass beide Pfade dasselbe Endergebnis erzeugen — obwohl die "Reihenfolge" der Prozesse in der Datei anders ist. Genau dafür existieren Delta-Zyklen.

## Wo es schiefgeht

Das Problem entsteht, wenn du **Signal**-Semantik mit "ich muss, dass das vor dem da innerhalb desselben Ticks passiert" mischst. Beispiel:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a wird am Ende des Ticks aktualisiert
    b <= a + 1;     -- b liest das ALTE a
  end if;
end process;
```

Wenn du angenommen hast, "Zeile 1 läuft zuerst, also ist `a` schon aktualisiert, wenn Zeile 2 läuft", liegst du falsch. `a` wird am Ende des Ticks aktualisiert. `b` liest den alten Wert von `a`. Ergebnis: beide erhöhen sich auf Basis alter Daten, nicht auf Basis der neuen Daten des anderen.

Ersetze die Signale durch Variablen, um C-artige Semantik zu erhalten:

```vhdl
process(clk)
  variable aa, bb : integer;
begin
  if rising_edge(clk) then
    aa := bb + 1;   -- sofort
    bb := aa + 1;   -- sieht das neue aa
  end if;
end process;
```

Jetzt funktioniert es so, wie es Softwareentwickler erwarten.

## Das Fazit

- Delta-Zyklen sind die Art des Simulators, nebenläufige Logik deterministisch zu machen.
- Signalzuweisungen werden **nach** den Delta-Zyklen wirksam. Variablen nicht.
- Wenn sich dein Testbench rund um eine Taktflanke "seltsam" verhält, vermute zuerst ein Delta-Zyklus-Reihenfolgeproblem, bevor du das Tool beschuldigst.

Die nächste Übung im Lehrplan zeigt eine kleine Race Condition in Aktion. Finde sie, benenne sie, behebe sie — du wirst die Regel nie wieder vergessen.
