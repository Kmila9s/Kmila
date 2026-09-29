# Voraussetzungen aus der diskreten Mathematik fürs Digitaldesign

Nicht alle Studierenden bringen denselben mathematischen Hintergrund mit. Diese Lektion ist eine 20-Minuten-Auffrischung der diskret-mathematischen Ideen, die im Digitaldesign ständig auftauchen — damit dich später nichts mehr stoppt, wenn eine Lektion sagt „stell dir das als Menge vor“ oder „der Zustandsraum ist das kartesische Produkt von …“.

## Mengen

Eine **Menge** ist eine ungeordnete Sammlung verschiedener Elemente: `{a, b, c}`. Im Digitaldesign begegnen dir Mengen an drei Stellen:

- Die Menge der **möglichen Werte**, die ein Signal annehmen kann (`std_logic` ist eine 9-elementige Menge: `{U, X, 0, 1, Z, W, L, H, -}`).
- Die Menge der **Ports**, die eine Entity deklariert.
- Die Menge der **Zustände**, in denen ein endlicher Automat sein kann.

Operationen, die vorkommen: **Vereinigung** (zwei Mengen zusammenführen), **Schnitt** (Gemeinsames behalten), **Differenz** (entfernen) und das **kartesische Produkt** — die Menge aller geordneten Paare aus zwei Mengen. Ein 2-Bit-Signal hat `{0, 1} × {0, 1}` = 4 mögliche Werte.

## Funktionen

Eine **Funktion** bildet jede Eingabe auf genau eine Ausgabe ab. Jede kombinatorische Schaltung ist eine Funktion: bei gegebenen Eingaben gibt es eine korrekte Ausgabe. `y <= a and b` ist die Funktion `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`.

Zwei Eigenschaften, die du unausgesprochen voraussetzt:

- **Total**: jede Eingabe hat eine Ausgabe (in einem kombinatorischen Prozess musst du auf jedem Pfad zuweisen).
- **Deterministisch**: gleiche Eingabe → gleiche Ausgabe (keine Race Conditions).

## Beweistechniken

Drei, auf die du dich stützen wirst:

- **Wahrheitstabellen**: erschöpfende Prüfung. Funktioniert bei kleinen Eingaberäumen; unser Lab 1 nutzt das.
- **Boolesche Algebra**: symbolische Vereinfachung (De Morgan, Distributivität, Komplement).
- **Induktion über die Bitbreite**: zeige, dass ein N-Bit-Addierer funktioniert, indem du den 1-Bit-Fall und den Schritt „wenn N geht, geht N+1“ beweist. Taucht in rekursiven Generics auf.

## Logik

Aussagenlogik IST Digitaldesign in anderer Notation:

| Mathematik   | VHDL          |
|--------------|---------------|
| ∧ (and)      | `and`         |
| ∨ (or)       | `or`          |
| ¬ (not)      | `not`         |
| ⊕ (xor)      | `xor`         |
| P ⇒ Q        | `(not P) or Q` |

Wenn du je einen Beweis mit diesen Operatoren geführt hast, hast du eine Schaltung geschrieben.

## Zählen

Zwei Formeln, die du brauchen wirst:

- Ein Port der Breite N hat **2^N** verschiedene Werte.
- Ein Automat mit K Zuständen lässt sich in **⌈log₂ K⌉** Flip-Flops codieren (binäre Codierung) oder in **K** Flip-Flops (One-Hot).

Genau deshalb gibt eine 4-Bit-Adresse 16 Speicherplätze, passt ein Ampel-FSM in 2 Flip-Flops, und ein 32-Bit-Zähler deckt 4 Milliarden Taktflanken ab, bevor er überläuft.

## Was du behalten solltest

- Mengen, Funktionen, kartesische Produkte — das Vokabular der Schaltungen.
- Boolesche Algebra ist Aussagenlogik, die man löten kann.
- `⌈log₂ K⌉` ist die Bitbreite, die du für K verschiedene Dinge brauchst.
