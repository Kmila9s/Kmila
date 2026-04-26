# Warum eine CPU pipelinen?

Deine Single-Cycle-8-Bit-CPU aus dem früheren Track funktioniert — aber sie ist in einer Dimension begrenzt, die auf echter Hardware enorm wichtig ist: **Taktfrequenz**.

## Die Single-Cycle-Obergrenze

In einem Single-Cycle-Design muss die Taktperiode lang genug sein, damit sich die Instruktion mit dem längsten kritischen Pfad ganz durchpropagieren kann: Fetch → Decode → Register-Read → ALU → Memory → Writeback, alles kombinatorisch. Wenn der längste Pfad 20 ns ist, ist dein maximaler Takt 50 MHz. Punkt.

## Pipelining unterteilt diesen Pfad

Zerlege den Pfad in N Stufen, getrennt durch Register:

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         Register zwischen den Stufen
```

Der Takt muss jetzt nur lang genug sein, damit die längste *Stufe* fertig wird, nicht der ganze Pfad. Fünf Stufen zu je ~4 ns ergeben einen 250-MHz-Takt. Der Durchsatz geht von 1 Instruktion / 20 ns auf 1 Instruktion / 4 ns — konzeptionell ein **5-facher Speedup** umsonst.

## Hazards — die Kosten

In der Praxis bekommst du nicht 5×. Drei Arten von Kollisionen:

- **Data Hazard** — Instruktion N+1 liest ein Register, das Instruktion N gerade noch schreibt. Lösung: Forwarding-Pfade oder Stall-Zyklen.
- **Control Hazard** — Instruktion N ist ein Sprung, und wir haben N+1 bereits geholt, bevor wir wussten, ob wir sie ausführen sollen. Lösung: Sprungvorhersage oder Pipeline-Flush.
- **Structural Hazard** — zwei Stufen brauchen dieselbe physische Ressource (z. B. einen Speicherport). Lösung: Ressource duplizieren oder serialisieren.

Jede folgende Lektion in diesem Modul implementiert eine Lösung nach der anderen auf der 8-Bit-CPU des Lehrplans, angefangen mit dem Forwarding.

## Was zu merken ist

- Pipelining tauscht **Latenz pro Instruktion** (höher) gegen **Durchsatz** (viel höher).
- Der Takt-Speedup ist durch die längste Stufe begrenzt, nicht durch die kürzeste.
- Hazards kosten Zyklen; Gegenmaßnahmen kosten Gatter. Echtes CPU-Design ist dieser Trade-off im großen Maßstab.

*Weitere Lektionen folgen — dieses Modul wird aktiv geschrieben.*
