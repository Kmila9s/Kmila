# Diskrete-Mathematik-Voraussetzungen für das Digitaldesign

Nicht jeder Studierende bringt denselben mathematischen Hintergrund mit. Diese Lektion ist eine 20-minütige Auffrischung der Ideen der diskreten Mathematik, die im Digitaldesign ständig auftauchen, damit wenn eine spätere Lektion sagt "denk dir das als eine Menge" oder "der Zustandsraum ist das kartesische Produkt von…", dich nichts aus der Bahn wirft.

## Mengen

Eine **Menge** ist eine ungeordnete Sammlung verschiedener Elemente: `{a, b, c}`. Im Digitaldesign triffst du auf Mengen an drei Stellen:

- Die Menge der **möglichen Werte**, die ein Signal annehmen kann (`std_logic` ist eine 9-elementige Menge: `{U, X, 0, 1, Z, W, L, H, -}`).
- Die Menge der **Ports**, die eine Entity deklariert.
- Die Menge der **Zustände**, in denen sich ein endlicher Automat befinden kann.

Operationen, die vorkommen werden: **Vereinigung** (zwei Mengen zusammenführen), **Schnitt** (Gemeinsames behalten), **Differenz** (entfernen) und das **kartesische Produkt** — die Menge aller geordneten Paare aus zwei Mengen. Ein 2-Bit-Signal hat `{0, 1} × {0, 1}` = 4 mögliche Werte.

## Funktionen

Eine **Funktion** bildet jede Eingabe auf genau eine Ausgabe ab. Jede kombinatorische Schaltung ist eine Funktion: gegeben die Eingänge, gibt es einen korrekten Ausgang. `y <= a and b` ist die Funktion `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`.

Zwei Eigenschaften, die du unbewusst aufrufen wirst:

- **Total**: Jede Eingabe hat eine Ausgabe (in einem kombinatorischen Prozess musst du auf jedem Pfad zuweisen).
- **Deterministisch**: gleiche Eingabe → gleiche Ausgabe (keine Race Conditions).

## Beweistechniken

Drei, auf die du dich stützen wirst:

- **Wahrheitstabellen**: erschöpfende Prüfung. Funktioniert für kleine Eingaberäume; unser Lab 1 nutzt das.
- **Boolesche Algebra**: symbolische Vereinfachung (De Morgan, Distributivität, Komplement).
- **Induktion über die Bitbreite**: Beweise, dass ein N-Bit-Addierer funktioniert, indem du den 1-Bit-Fall zeigst und den "wenn N funktioniert, dann N+1"-Schritt. Taucht in rekursiven Generics auf.

## Logik

Aussagenlogik IST Digitaldesign mit anderer Notation:

| Mathe       | VHDL          |
|-------------|---------------|
| ∧ (und)     | `and`         |
| ∨ (oder)    | `or`          |
| ¬ (nicht)   | `not`         |
| ⊕ (xor)     | `xor`         |
| P ⇒ Q       | `(not P) or Q` |

Wenn du einen Beweis mit diesen Operatoren geschrieben hast, hast du eine Schaltung geschrieben.

## Zählen

Zwei Formeln, die du nutzen wirst:

- Ein Port der Breite N hat **2^N** verschiedene Werte.
- Ein Zustandsautomat mit K Zuständen kann in **⌈log₂ K⌉** Flipflops kodiert werden (für binäre Kodierung) oder in **K** Flipflops (One-Hot).

Das ist der Grund, warum eine 4-Bit-Adresse 16 Speicherplätze ergibt, warum eine Ampel-FSM in 2 Flipflops passt und warum ein 32-Bit-Zähler 4 Milliarden Taktflanken abdeckt, bevor er überläuft.

## Was zu merken ist

- Mengen, Funktionen, kartesische Produkte — das Vokabular der Schaltungen.
- Boolesche Algebra ist Aussagenlogik, die man löten kann.
- `⌈log₂ K⌉` ist die Bitbreite, die du für K verschiedene Dinge brauchst.
