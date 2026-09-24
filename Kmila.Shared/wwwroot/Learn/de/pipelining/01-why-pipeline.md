# Warum eine CPU pipelinen?

Deine Single-Cycle-8-Bit-CPU aus dem vorigen Track funktioniert — aber sie ist in einer Dimension begrenzt, die auf echter Hardware enorm zählt: der **Taktfrequenz**.

## Die Single-Cycle-Decke

In einem Single-Cycle-Entwurf muss die Taktperiode lang genug sein, damit der längste-kritische-Pfad-Befehl komplett durchpropagiert: Fetch → Decode → Register-Read → ALU → Memory → Writeback, alles kombinatorisch. Wenn der längste Pfad 20 ns dauert, ist dein Maximaltakt 50 MHz. Punkt.

## Pipelining bedeutet, diesen Pfad zu teilen

Zerlege den Pfad in N Stufen, getrennt durch Register:

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         Register zwischen den Stufen
```

Der Takt muss nun nur lang genug sein, damit die längste *Stufe* fertig wird, nicht der ganze Pfad. Fünf Stufen à ~4 ns ergeben einen 250-MHz-Takt. Der Durchsatz steigt von 1 Befehl / 20 ns auf 1 Befehl / 4 ns — **5-fache Beschleunigung**, konzeptuell umsonst.

## Hazards — der Preis

In der Praxis kriegst du nicht 5×. Drei Kollisionsarten:

- **Daten-Hazard** — Befehl N+1 liest ein Register, das Befehl N noch schreibt. Lösung: Forwarding-Pfade oder Stall-Zyklen.
- **Control-Hazard** — Befehl N ist ein Sprung, und wir haben N+1 schon geholt, bevor wir wissen, ob er ausgeführt wird. Lösung: Branch Prediction oder Pipeline-Flush.
- **Struktureller Hazard** — zwei Stufen brauchen dieselbe physische Ressource (z. B. einen Speicher-Port). Lösung: Ressource duplizieren oder serialisieren.

Jede folgende Lektion in diesem Modul implementiert eine Lösung nach der anderen auf der 8-Bit-CPU des Curriculums, beginnend mit Forwarding.

## Was du behalten solltest

- Pipelining tauscht **Latenz pro Befehl** (höher) gegen **Durchsatz** (viel höher).
- Die Takt-Beschleunigung ist durch die längste Stufe gedeckelt, nicht durch die kürzeste.
- Hazards kosten Zyklen; Gegenmaßnahmen kosten Gatter. Reales CPU-Design ist dieser Trade-off im großen Stil.

*Weitere Lektionen folgen — dieses Modul ist in aktiver Bearbeitung.*
