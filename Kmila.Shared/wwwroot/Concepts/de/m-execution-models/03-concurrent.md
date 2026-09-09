# Nebenläufig: alles feuert gleichzeitig

Außerhalb jedes `process` — direkt in einer `architecture` zwischen
`begin` und `end` — kannst du **nebenläufige Signalzuweisungen**
schreiben. Sie sehen aus wie sequenzielle Zuweisungen, folgen aber
einer völlig anderen Regel:

> **Alle nebenläufigen Anweisungen, die auf ein Ereignis reagieren,
> feuern im selben Delta-Zyklus.** Die Quelltextreihenfolge ist egal.

Stell dir ein Gebäude mit vier Lichtschaltern in verschiedenen Räumen
vor. Wenn der Strom kommt, greift der Zustand jedes Schalters im
selben Augenblick — es ist egal, welchen Raum der Elektriker zuerst
verdrahtet hat.

Der Player unten hat vier nebenläufige Anweisungen, die vier
verschiedene Ausgänge aus denselben Eingängen `a` und `b` treiben.
Drück Play und beobachte, wie alle vier Ausgangszeilen im selben
Schritt aktualisiert werden.

## Zwei Wege, das zu prüfen

- **Vertausch mental zwei Zeilen** — ändert sich das Ergebnis? Nein.
  Probier's.
- **Entfern die Eingänge aus der Architektur und schau, was mit ALLEN
  Ausgängen passiert.** Sie gehen gemeinsam zurück auf `U`, weil es
  keine Sequenz gibt — es ist eine parallele Auswertung pro
  Delta-Zyklus.

## Nebenläufig vs. sequenziell — wann nimmst du was?

| Anwendungsfall | Wählen |
|---|---|
| Kombinatorische Logik (Gatter, Mux, Arithmetik ohne Takt) | **Nebenläufig** |
| Getaktetes Verhalten (Register, FSMs, Zähler) | **Sequenziell** (`process` mit `rising_edge(clk)`) |
| Mehrere Signale lesen, rechnen, ein Ergebnis ausgeben | Beides geht — nebenläufig liest sich mehr wie eine Schaltung, sequenziell mehr wie ein Programm |

> **Probier's:** halt den Player in dem Moment an, in dem alle vier
> Zeilen hervorgehoben sind. Dieser einzelne Augenblick ist **ein
> Delta-Zyklus**. Alle vier Zuweisungen werden zu genau derselben
> Simulationszeit ausgewertet.
