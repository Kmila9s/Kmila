# Delta-Zyklen und Race Conditions

Ein Simulator muss „zwei Dinge passieren gleichzeitig“ eine deterministische Reihenfolge geben. VHDLs Antwort ist der **Delta-Zyklus**: ein unendlich kleines Zeitstück, das Ursache von Wirkung trennt, ohne die Uhr an der Wand vorzurücken. Er ist im Simulationszyklus des VHDL-Standards genau definiert, und ihn zu verstehen ist das, was „seltsames“ Testbench-Verhalten plötzlich Sinn ergeben lässt.

## Was ein Delta-Zyklus ist

Die Simulationszeit schreitet in echten Schritten voran (gemessen in ns, µs, ms). Aber innerhalb eines einzigen Zeitschritts passieren Änderungen in **Delta-Zyklus**-Reihenfolge:

- δ₀: eine Zuweisung plant eine Signaländerung ein.
- δ₁: die Änderung tritt in Kraft; jeder Prozess, der auf das Signal sensitiv ist, sieht jetzt neue Eingaben und wacht auf.
- δ₂: diese geweckten Prozesse planen *ihre* Änderungen ein.
- δ₃: diese Änderungen treten in Kraft; weitere Prozesse können aufwachen.
- …

Jedes Delta ist „null echte Zeit“, repräsentiert aber trotzdem einen Kausalschritt. Der Simulator kreiselt durch die Deltas, bis sich kein Signal mehr ändert — die Schaltung ist **stabil** — und erst dann rückt die echte Simulationszeit zum nächsten Event vor.

## Der Simulationszyklus, etwas präziser

Ein Durchlauf durch die Engine ist: (1) rücke die Zeit zum nächsten eingeplanten Event vor; (2) aktualisiere alle für jetzt eingeplanten Signale; (3) führe jeden Prozess, der auf eines dieser Signale gewartet hat, bis zum Ende aus, was neue Änderungen einplanen kann; (4) wenn irgendeine davon für die *aktuelle* Zeit ist, mach einen weiteren Delta-Zyklus; sonst rücke die Zeit vor. Die Signalaktualisierungen in Schritt 2 passieren alle *bevor* ein Prozess sie in Schritt 3 liest — das ist der Mechanismus hinter „Signale aktualisieren am Ende des Schritts“.

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

Ohne Delta-Zyklen müsste der Simulator eine willkürliche Reihenfolge wählen — wird `a` vor `b` aktualisiert? Danach? Delta-Zyklen umgehen das: beide Zuweisungen werden bei δ₀ eingeplant und beide werden bei δ₁ wirksam. Jeder Prozess, der `a` oder `b` liest, sieht beide neuen Werte zusammen. Das Ergebnis hängt nicht länger von der Reihenfolge ab, in der der Simulator die Dinge zufällig besucht.

## Eine Datenfluss-Kette

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

Wenn `a` sich ändert, feuert der erste Prozess und plant `b` ein. Beim nächsten Delta hat `b` seinen neuen Wert, was den zweiten Prozess weckt, der `c` einplant. Beim Delta danach stimmt `c`. Beachte, dass `c` `a` nach **zwei Delta-Zyklen** einholt — echte Zeit bewegte sich nie, aber die Kausalität blieb der Reihe nach erhalten. Vertausch die beiden Prozesse in der Datei und nichts ändert sich: Die Delta-Reihenfolge garantiert dasselbe Endergebnis. Genau dafür gibt es Delta-Zyklen.

## Die Race-Condition-Falle

Das Problem entsteht, wenn du **Signal**-Semantik mit der Annahme mischst, dass „das muss vor jenem im selben Tick passieren“:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a wird am Ende des Schritts aktualisiert
    b <= a + 1;     -- b liest das ALTE a
  end if;
end process;
```

Wenn du annimmst „Zeile 1 läuft zuerst, also ist `a` bei Zeile 2 schon aktualisiert“, liegst du falsch. Beide rechten Seiten werden mit den **alten** Werten ausgewertet, dann werden beide Ergebnisse eingeplant. `b` liest den alten Wert von `a`, nicht den frisch berechneten.

Ersetze die Signale durch Variablen, um C-Stil-„lies, was du gerade geschrieben hast“-Semantik zu bekommen:

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

Jetzt funktioniert es so, wie Software-Programmierer es erwarten. (Und die Hardware unterscheidet sich: die Signal-Version baut zwei parallele Addierer aus den alten Registern; die Variablen-Version baut eine abhängige Kette.)

## Ein klassischer echter Bug: die vergessene Flanke

Delta-Denken erklärt auch, warum dieser Testbench-Stimulus sich daneben benimmt:

```vhdl
clk  <= not clk after 10 ns;
-- woanders, in einem Prozess ohne wait:
data <= next_value;   -- dasselbe Delta wie die Flanke, die das DUT abtastet?
```

Wenn ein Stimulus-Signal sich im *selben* Delta ändert, in dem das DUT taktet, ist es fragil, ob das DUT den alten oder neuen Wert sieht. Der Fix ist, den Stimulus mit einem sicheren Abstand *weg* von der aktiven Flanke zu ändern (oder `wait until rising_edge(clk)` und dann eine kleine Verzögerung zu nutzen). Viele Meldungen der Art „es besteht in der Sim, aber die Reihenfolge fühlt sich zufällig an“ gehen darauf zurück.

## Deltas in den Tools erkennen

GHDL (die Engine, die Kmila ansteuert) und GTKWave legen beide die Delta-Schritte offen — ein Signal kann mehrere Delta-Zyklen brauchen, um an einem einzigen Zeitpunkt zu seinem Endwert durchzusickern. Wenn ein Waveform einen Glitch zeigt, der „null Zeit“ einnimmt, siehst du Delta-Zwischenwerte, nicht echtes Hardware-Verhalten.

## Zum Mitnehmen

- Delta-Zyklen sind die Art des Simulators, nebenläufige Logik deterministisch zu machen, ohne falsche Zeit zu erfinden.
- Signalzuweisungen werden **nach** dem aktuellen Delta wirksam; Variablen nicht — sie sind sofort.
- Rechte Seiten werden mit den *alten* Signalwerten ausgewertet, dann eingeplant.
- Wenn sich deine Testbench um eine Taktflanke „seltsam“ verhält, vermute ein Delta-Reihenfolgeproblem, bevor du dem Tool die Schuld gibst.

## Was du behalten solltest

- Ein Delta-Zyklus ist null echte Zeit, aber ein Kausalschritt; der Simulator läuft Deltas, bis die Schaltung stabil ist, dann rückt die Zeit vor.
- Signale aktualisieren zwischen den Deltas (du liest also alte Werte im selben Durchlauf); Variablen aktualisieren sofort.
- Rück den Testbench-Stimulus weg von der aktiven Taktflanke, um Races zu vermeiden.

Die nächste Übung zeigt eine kleine Race Condition in Aktion. Finde sie, benenne sie, fixe sie — du wirst die Regel nie wieder vergessen.
