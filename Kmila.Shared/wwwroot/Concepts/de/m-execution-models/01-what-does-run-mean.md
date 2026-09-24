# Was heißt eigentlich „ausführen"?

Wenn du in Kmila **Run** drückst, schaltet der Simulator kein echtes FPGA
ein. Er geht dein VHDL Schritt für Schritt durch — aber **wie** er das
tut, hängt davon ab, wo der Code lebt.

Es gibt drei Ausführungsmodelle, die ständig verwechselt werden:

| Modell | Bedeutung | Wo du das in VHDL siehst |
|---|---|---|
| **Sequenziell** | Eine Anweisung nach der anderen, in Quelltextreihenfolge. | Innerhalb eines `process`-Körpers. |
| **Nebenläufig** | Viele Anweisungen werden „gleichzeitig" ausgewertet — die Reihenfolge im File spielt keine Rolle. | Nebenläufige Anweisungen in einer Architektur (zwischen `begin` und `end`). |
| **Parallel** | Mehrere Dinge passieren wirklich im selben Moment realer Wanduhrzeit. | **Nur auf echter Hardware.** Niemals in der Simulation. |

Das Beispiel unten ist absichtlich winzig — ein `process`, der zwei
Eingänge `a` und `b` liest und einen Ausgang `sum` schreibt. Drück ▶
im Player und beobachte, welche Zeile der Simulator gerade „ansieht".

```text
Sequenziell:  Zeile 1  →  Zeile 2  →  Zeile 3  …
Nebenläufig:  Zeilen 1, 2 und 3 werden in diesem Augenblick ausgewertet
Parallel:     gibt's in der Simulation nicht. Nur auf dem FPGA selbst.
```

## Warum das wichtig ist

Anfänger lesen `sum <= b;` und denken, der Wert von `sum` würde
**sofort** zu `b`, so wie es `sum = b;` in C wäre. Tut er nicht. Die
Zuweisung **legt eine Transaktion an**, und der neue Wert wird erst
sichtbar, nachdem der aktuelle Delta-Zyklus endet.

Der Player rechts hat eine hartcodierte Ausführung — es läuft keine
echte Simulation. Das hält die Erklärung ehrlich: jeder Schritt trägt
eine Lehrnote, statt dass du erraten musst, was der Simulator gerade
dachte.

> **Probier's:** geh manuell vorwärts und schau, wie `sum` selbst nach
> Schritt 3 auf `U` („uninitialisiert") stehen bleibt — es ändert sich
> erst, wenn der Delta-Zyklus in Schritt 4 schließt.
