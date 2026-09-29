# Delta-Zyklen und Race Conditions

Ein Simulator muss „zwei Dinge passieren gleichzeitig“ deterministisch ordnen. VHDLs Antwort ist der **Delta-Zyklus**: ein unendlich kleines Zeitstück, das Ursache von Wirkung trennt.

## Was ein Delta-Zyklus ist

Die Simulationszeit schreitet in echten Schritten voran (gemessen in ns, µs, ms). Aber innerhalb eines einzigen Zeitschritts können Änderungen in **Delta-Zyklus**-Reihenfolge passieren:

- δ₀: du planst eine Signaländerung ein.
- δ₁: die Änderung tritt in Kraft; jeder Prozess, der auf das Signal sensitiv ist, hat jetzt neue Eingaben.
- δ₂: diese Prozesse planen ihre Änderungen ein.
- δ₃: diese Änderungen treten in Kraft.
- …

Jedes Delta ist „null echte Zeit“, repräsentiert aber einen Kausalschritt. Der Simulator kreiselt durch Deltas, bis nichts mehr zu aktualisieren ist — erst dann schreitet die echte Zeit voran.

## Warum es das gibt

Betrachte zwei Signale, die beide auf derselben Taktflanke aktualisieren:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

Ohne Delta-Zyklen müsste der Simulator eine willkürliche Reihenfolge wählen — wird `a` vor `b` aktualisiert? Danach? Im exakt selben Moment? Delta-Zyklen umgehen das: beide Zuweisungen werden bei δ₀ eingeplant und beide werden bei δ₁ wirksam. Jeder Prozess, der `a` oder `b` liest, sieht beide neuen Werte gleichzeitig.

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

Wenn `a` sich ändert, feuert der erste Prozess und plant `b` ein. Bei δ₁ hat `b` seinen neuen Wert. Der zweite Prozess feuert, plant `c` ein. Bei δ₂ hat `c` den richtigen Wert.

Aber dreh jetzt die beiden Prozesse um. Ändert sich etwas? **Nein.** Delta-Zyklen garantieren, dass beide Pfade dasselbe Endergebnis liefern — auch wenn die „Reihenfolge“ der Prozesse in der Datei anders ist. Genau dafür gibt es Delta-Zyklen.

## Wo es schiefgeht

Das Problem entsteht, wenn du **Signal**-Semantik mit „das muss vor jenem im selben Tick passieren“ mischst. Beispiel:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a wird am Ende des Ticks aktualisiert
    b <= a + 1;     -- b liest das ALTE a
  end if;
end process;
```

Wenn du annimmst „Zeile 1 läuft zuerst, also ist `a` bei Zeile 2 schon aktualisiert“, liegst du falsch. `a` wird am Ende des Ticks aktualisiert. `b` liest den alten Wert von `a`. Ergebnis: beide inkrementieren auf alten Daten basierend, nicht auf den neuen Daten des anderen.

Ersetze die Signale durch Variablen, um C-Stil-Semantik zu kriegen:

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

Jetzt funktioniert es so, wie Software-Programmierer es erwarten.

## Zum Mitnehmen

- Delta-Zyklen sind die Art des Simulators, nebenläufige Logik deterministisch zu machen.
- Signalzuweisungen werden **nach** Delta-Zyklen wirksam. Variablen nicht.
- Wenn sich deine Testbench um eine Taktflanke „seltsam“ verhält, vermute zuerst ein Delta-Zyklus-Reihenfolgeproblem, bevor du dem Tool die Schuld gibst.

Die nächste Übung im Curriculum zeigt eine kleine Race Condition in Aktion. Finde sie, benenne sie, fixe sie — du wirst die Regel nie wieder vergessen.
