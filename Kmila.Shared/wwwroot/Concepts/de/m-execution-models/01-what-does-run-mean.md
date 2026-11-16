# Was heißt eigentlich „ausführen"?

Wenn du in Kmila **Run** drückst, schaltet der Simulator kein echtes FPGA ein. Er geht dein VHDL Schritt für Schritt durch — aber **wie** er das tut, hängt davon ab, wo der Code lebt.

Es gibt drei Ausführungsmodelle, die ständig verwechselt werden:

| Modell | Bedeutung | Wo du das in VHDL siehst |
|---|---|---|
| **Sequenziell** | Eine Anweisung nach der anderen, in Quelltextreihenfolge. | Innerhalb eines `process`-Körpers. |
| **Nebenläufig** | Viele Anweisungen werden „gleichzeitig" ausgewertet — die Reihenfolge im File spielt keine Rolle. | Nebenläufige Anweisungen in einer Architektur (zwischen `begin` und `end`). |
| **Parallel** | Mehrere Dinge passieren wirklich im selben Moment realer Wanduhrzeit. | **Nur auf echter Hardware.** Niemals in der Simulation. |

Das Beispiel unten ist absichtlich winzig — ein `process`, der zwei Eingänge `a` und `b` liest und einen Ausgang `sum` schreibt. Drück ▶ im Player und beobachte, welche Zeile der Simulator gerade „ansieht".

```text
Sequenziell:  Zeile 1  →  Zeile 2  →  Zeile 3  …
Nebenläufig:  Zeilen 1, 2 und 3 werden in diesem Augenblick ausgewertet
Parallel:     gibt's in der Simulation nicht. Nur auf dem FPGA selbst.
```

<!-- exec-fig:start -->
## Die Restaurant-Analogie

Stell dir ein Restaurant mit **10 Tischen** vor. Die **Kellnerin** ist das, was die Arbeit erledigt (eine CPU oder der Simulator von Kmila). Jeder **Tisch** ist eine Aufgabe, die Aufmerksamkeit braucht, und jedes **Gericht** braucht eine Weile in der Küche. Mit den Knöpfen über der Zeichnung änderst du, wie das Restaurant läuft:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=seq,poll,par" title="Restaurant: eine Kellnerin, eine Kellnerin, die zwischen Tischen wechselt, fünf Kellnerinnen" loading="lazy"></iframe></div>

- **1 →** — *sequenziell*: eine Kellnerin bedient einen Tisch nach dem anderen. Sie nimmt die Bestellung auf und **bleibt dort stehen, bis das Gericht kommt**, auch wenn neun andere Tische warten.
- **1 ↻** — *nebenläufig*: immer noch eine Kellnerin, aber wenn das Gericht nicht fertig ist, **geht sie weiter**: nimmt die nächste Bestellung auf, schaut beim ersten Tisch wieder vorbei und serviert, was fertig ist.
- **5 ‖** — *parallel*: fünf Kellnerinnen, je zwei Tische, alle arbeiten **im selben Moment**.

So liest du das Diagramm: eine Zeile pro Tisch; **blauer Balken** = die Kellnerin ist an diesem Tisch, **rote Linie** = das Gericht wird gekocht, **gestrichelt** = fertig, aber noch nicht serviert, **grüner Punkt** = serviert. Die Anzeige zeigt <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><circle cx="8" cy="9" r="5.5"/><path d="M8 9V6.2M6.5 1.8H9.5M8 1.8V3.5"/></svg> vergangene Zeit, <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M4.5 2H11.5M4.5 14H11.5M5.2 2C5.2 5.5 10.8 6.8 10.8 14M10.8 2C10.8 5.5 5.2 6.8 5.2 14"/></svg> Zeit, in der die Kellnerin untätig wartet, ↻ „Ist es fertig?"-Kontrollen und ✓ bediente Tische. Nach jedem Durchlauf bleibt eine gestrichelte Ziellinie im Diagramm, damit du die Modi vergleichen kannst.

| Restaurant | Ausführungsmodell |
|---|---|
| Kellnerin | Das, was ausführt: ein CPU-Thread oder der Simulator |
| Tisch | Ein `process` (oder eine nebenläufige Anweisung) |
| Gericht in der Küche | Vergehende Zeit (`after 10 ns`, auf eine Taktflanke warten…) |
| Gericht fertig | Ein **Ereignis** (Event): worauf der Prozess gewartet hat, ist eingetreten |
| Fünf Kellnerinnen | Echte Hardware: ein FPGA, in dem jedes Stück Schaltung seine eigene „Kellnerin" ist |

> **Achte darauf:** Die nebenläufige Kellnerin läuft nicht schneller, sie **steht nur nie untätig herum**. Sequenziell dauert etwa 70 s, nebenläufig etwa 37 s, parallel etwa 12 s.
<!-- exec-fig:end -->

## Warum das wichtig ist

Anfänger lesen `sum <= b;` und denken, der Wert von `sum` würde **sofort** zu `b`, so wie es `sum = b;` in C wäre. Tut er nicht. Die Zuweisung **legt eine Transaktion an**, und der neue Wert wird erst sichtbar, nachdem der aktuelle Delta-Zyklus endet.

Der Player rechts hat eine hartcodierte Ausführung — es läuft keine echte Simulation. Das hält die Erklärung ehrlich: jeder Schritt trägt eine Lehrnote, statt dass du erraten musst, was der Simulator gerade dachte.

> **Probier's:** geh manuell vorwärts und schau, wie `sum` selbst nach Schritt 3 auf `U` („uninitialisiert") stehen bleibt — es ändert sich erst, wenn der Delta-Zyklus in Schritt 4 schließt.
