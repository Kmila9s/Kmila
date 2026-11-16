# Nebenläufig: alles feuert gleichzeitig

Außerhalb jedes `process` — direkt in einer `architecture` zwischen `begin` und `end` — kannst du **nebenläufige Signalzuweisungen** schreiben. Sie sehen aus wie sequenzielle Zuweisungen, folgen aber einer völlig anderen Regel:

> **Alle nebenläufigen Anweisungen, die auf ein Ereignis reagieren, feuern im selben Delta-Zyklus.** Die Quelltextreihenfolge ist egal.

Stell dir ein Gebäude mit vier Lichtschaltern in verschiedenen Räumen vor. Wenn der Strom kommt, greift der Zustand jedes Schalters im selben Augenblick — es ist egal, welchen Raum der Elektriker zuerst verdrahtet hat.

Der Player unten hat vier nebenläufige Anweisungen, die vier verschiedene Ausgänge aus denselben Eingängen `a` und `b` treiben. Drück Play und beobachte, wie alle vier Ausgangszeilen im selben Schritt aktualisiert werden.

<!-- exec-fig:start -->
## Zurück ins Restaurant: nachschauen oder auf die Klingel hören

Hier gibt es immer noch **nur eine Kellnerin**, also ist das Nebenläufigkeit, keine Parallelität. Vergleiche zwei Arten, zwischen den Tischen zu wechseln:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=poll,bell&mode=poll" title="Restaurant: eine Kellnerin, die jeden Tisch kontrolliert, oder die auf Klingeln reagiert" loading="lazy"></iframe></div>

- **1 ↻** — sie macht die Runde und **schaut** an jedem Tisch nach, ob fertig oder nicht. Jedes ↻ in der Anzeige ist ein umsonst gemachter Weg.
- **1 <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M3 12H13M4.6 12C4.6 6 5.8 3 8 3S11.4 6 11.4 12M6.6 14.2A1.5 1.5 0 0 0 9.4 14.2"/></svg>** — sie nimmt alle Bestellungen auf und geht dann **nur dorthin, wo eine Klingel läutet** (die Tischklingel leuchtet, sobald das Gericht fertig ist).

Der VHDL-Simulator arbeitet auf die zweite Art. Er führt nicht ständig alle Anweisungen neu aus, sondern weckt nur die Prozesse, deren **Sensitivitätsliste** ein **Ereignis** bekommen hat. Die Klingel ist die Sensitivitätsliste; das Läuten ist das Ereignis. Gleiche Kellnerin, gleiche Tische, weniger Schritte (etwa 23 s statt 37 s, und 0 umsonst gemachte Kontrollen).
<!-- exec-fig:end -->

<!-- exec-fig2:start -->
## Der Fotograf: ein Foto, ein Blitz

Warum ist die Reihenfolge im Code egal? Weil in einem Delta-Zyklus alle nebenläufigen Anweisungen dasselbe **Foto** der Eingänge lesen und niemand das Ergebnis der anderen sieht, bevor der **Blitz** kommt.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-photo.html" title="Fotostudio: vier Tische schreiben von einem Foto ab; der Blitz klappt alle vier Tafeln gleichzeitig um" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- Die Kamera löst aus: Das Foto friert `a` und `b` ein.
- Jeder Tisch (einer pro Anweisung) schreibt seinen **Entwurf** nach demselben Foto.
- Der Blitz ist das Ende des Deltas: Alle vier Ausgangstafeln wechseln **gleichzeitig**.
<!-- exec-fig2:end -->

## Zwei Wege, das zu prüfen

- **Vertausch mental zwei Zeilen** — ändert sich das Ergebnis? Nein. Probier's.
- **Entfern die Eingänge aus der Architektur und schau, was mit ALLEN Ausgängen passiert.** Sie gehen gemeinsam zurück auf `U`, weil es keine Sequenz gibt — es ist eine parallele Auswertung pro Delta-Zyklus.

## Nebenläufig vs. sequenziell — wann nimmst du was?

| Anwendungsfall | Wählen |
|---|---|
| Kombinatorische Logik (Gatter, Mux, Arithmetik ohne Takt) | **Nebenläufig** |
| Getaktetes Verhalten (Register, FSMs, Zähler) | **Sequenziell** (`process` mit `rising_edge(clk)`) |
| Mehrere Signale lesen, rechnen, ein Ergebnis ausgeben | Beides geht — nebenläufig liest sich mehr wie eine Schaltung, sequenziell mehr wie ein Programm |

> **Probier's:** halt den Player in dem Moment an, in dem alle vier Zeilen hervorgehoben sind. Dieser einzelne Augenblick ist **ein Delta-Zyklus**. Alle vier Zuweisungen werden zu genau derselben Simulationszeit ausgewertet.
