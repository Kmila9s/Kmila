# Diskrete Mathematik als Grundlage für Digitaldesign

Nicht jeder bringt denselben mathematischen Hintergrund mit. Diese Lektion ist eine 25-minütige Auffrischung der Ideen aus der diskreten Mathematik, die im Digitaldesign ständig auftauchen – damit dich nichts aus dem Konzept bringt, wenn eine spätere Lektion sagt „stell dir das als Menge vor" oder „der Zustandsraum ist das kartesische Produkt aus …". Alles hier lässt sich direkt auf die vier Einheiten des ESCOM-Kurses *Matemáticas Discretas* abbilden – Logik, Mengenlehre, ganze Zahlen und diskrete Strukturen – und diesmal lesen wir jede davon als Hardware.

## Logik ist Digitaldesign mit anderen Symbolen

Die Aussagenlogik (Einheit I des Kurses) ist dieselbe Algebra, der auch deine Gatter gehorchen – nur mit anderen Zeichen geschrieben. Eine **Aussage** ist ein Satz, der entweder wahr oder falsch ist – genau die zwei Werte, die eine Leitung tragen kann.

| Mathe       | Gelesen als      | VHDL            |
|-------------|------------------|-----------------|
| ∧ (und)     | „P und Q"        | `and`           |
| ∨ (oder)    | „P oder Q"       | `or`            |
| ¬ (nicht)   | „nicht P"        | `not`           |
| ⊕ (xor)     | „P xor Q"        | `xor`           |
| P ⇒ Q       | „wenn P, dann Q" | `(not P) or Q`  |

Die letzte Zeile solltest du dir merken. Die Implikation **P ⇒ Q ist logisch äquivalent zu ¬P ∨ Q** – es gibt kein `implies`-Gatter, weil du nie eines brauchst. Zwei weitere Äquivalenzen, auf die du ständig zurückgreifst, sind die **Gesetze von De Morgan**:

- ¬(P ∧ Q) ≡ ¬P ∨ ¬Q  →  `not (a and b) = (not a) or (not b)`
- ¬(P ∨ Q) ≡ ¬P ∧ ¬Q  →  `not (a or b) = (not a) and (not b)`

Dank De Morgan kannst du eine Negation (ein „Bubble") durch ein Gatter hindurchschieben und dabei AND und OR tauschen. Synthesewerkzeuge tun das tausendfach, um deine Logik auf die Zellen abzubilden, die eine Bibliothek tatsächlich bereitstellt.

**Achtung:** Die Umkehrung (Q ⇒ P), die Inversion (¬P ⇒ ¬Q) und die Kontraposition (¬Q ⇒ ¬P) sind *nicht* austauschbar. Nur die **Kontraposition ist äquivalent** zum Original – eine Unterscheidung, die der Kurs in Einheit I einbläut, und der Grund, warum ein unachtsames „if-else" die falsche Schaltung beschreiben kann.

## Boolesche Algebra: die Logik, die du löten kannst

Einheit IV nennt das *álgebras y funciones booleanas*, und sie ist der formale Kern des kombinatorischen Entwurfs. Eine **boolesche Funktion** von n Variablen ist eine Abbildung von {0,1}ⁿ nach {0,1}. Für n Eingänge gibt es genau `2^(2^n)` davon – 16 verschiedene Funktionen mit zwei Eingängen, weshalb die sechs benannten Gatter plus Konstanten alles abdecken.

Jede solche Funktion hat zwei kanonische Formen:

- **Summe der Minterme (SOP):** Verknüpfe mit OR je einen Produktterm für jede Eingangszeile, die eine `1` liefert.
- **Produkt der Maxterme (POS):** Verknüpfe mit AND je einen Summenterm für jede Zeile, die eine `0` liefert.

Durchgerechnetes Beispiel – die Mehrheitsfunktion (Ausgang `1`, wenn zwei oder drei Eingänge `1` sind):

```
f(a,b,c) = (a and b) or (a and c) or (b and c)
```

Diese drei Produkte sind die Minterme. Du kannst den Ausdruck anschließend mit den booleschen Axiomen verkleinern – Identität, Komplement (`a or not a = 1`), Distributivität, Absorption (`a or (a and b) = a`) – oder mit einem **Karnaugh-Diagramm**, dem visuellen Minimierer aus Einheit IV. Weniger Literale bedeuten weniger Transistoren.

```vhdl
y <= (a and b) or (a and c) or (b and c);  -- drei Gatter, als Leitungen ausgewertet
```

**Achtung:** Für *Fläche* zu minimieren ist nicht dasselbe wie für *Geschwindigkeit* zu minimieren, und eine Synthese kann deine handoptimierte Algebra wieder rückgängig machen, um beides auszubalancieren. Schreib klare Logik und lass das Werkzeug optimieren.

## Mengen: das Vokabular für Signale, Ports und Zustände

Eine **Menge** (Einheit II) ist eine ungeordnete Sammlung verschiedener Elemente: `{a, b, c}`. Im Digitaldesign begegnen sie dir überall:

- Die **Werte, die ein Signal annehmen kann.** `std_logic` ist eine Menge aus 9 Elementen – `{U, X, 0, 1, Z, W, L, H, -}` – und deckt uninitialisiert, unbekannt, die beiden starken Logikpegel, Hochohmigkeit, schwache Pegel und Don't-Care ab.
- Die **Ports**, die eine Entity deklariert, und die **Zustände**, die ein Zustandsautomat einnehmen kann.

Die wiederkehrenden Operationen sind **Vereinigung** (∪), **Schnitt** (∩), **Differenz** (∖) und **Komplement**. Zwei Zählwerkzeuge aus Einheit II verdienen sich ihren Platz:

- **Kartesisches Produkt** A × B: die Menge aller geordneten Paare. Ein 2-Bit-Signal lebt in `{0,1} × {0,1}`, hat also 4 Werte; ein n-Bit-Vektor lebt in `{0,1}ⁿ` und liefert **2ⁿ**. Genau deshalb erreicht eine 4-Bit-Adresse 16 Speicherplätze.
- **Potenzmenge** 𝒫(A): die Menge aller Teilmengen von A, mit der Mächtigkeit **2^|A|**. Die Wahrheitstabelle einer Funktion mit n Eingängen ist eine Aufzählung von 𝒫 über die Eingänge – dieselben 2ⁿ Zeilen.

Zum Abmessen überlappender Mengen liefert das **Prinzip von Inklusion und Exklusion** |A ∪ B| = |A| + |B| − |A ∩ B|.

## Funktionen und Relationen

Jede kombinatorische Schaltung **ist eine Funktion**: Zu gegebenen Eingängen gibt es genau einen korrekten Ausgang. `y <= a and b` realisiert `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`. Zwei Eigenschaften (Einheit IV, *propiedades de las funciones*), die du aufrufst, ohne sie zu benennen:

- **Total** – jeder Eingang hat einen Ausgang. In einem kombinatorischen `process` musst du den Ausgang auf *jedem* Pfad zuweisen, sonst leitet das Werkzeug ein unerwünschtes Latch ab, um den fehlenden Fall zu „merken".
- **Deterministisch** – derselbe Eingang liefert immer denselben Ausgang, ohne Race Conditions.

Sequentielle Logik erweitert das zu **Relationen** (Einheit IV): Eine Folgezustands-Relation kann einen Zustand auf mehrere abbilden, was genau einem nichtdeterministischen Zustandsautomaten entspricht – meist ein Fehler, gelegentlich eine Spezifikation, die du zu einer deterministischen Maschine verfeinerst.

## Ganze Zahlen, Basen und Induktion

Einheit III ist Zahlentheorie, und ihr Abschnitt *sistemas de numeración* ist reine Digital-Grundlagenarbeit: der Basisumrechnungs-Algorithmus, der enge Zusammenhang zwischen Binär und Hexadezimal (eine Hex-Ziffer = ein 4-Bit-Nibble) und die Arithmetik zur Basis *b*. Falls sich Basisumrechnung oder Zweierkomplement noch wackelig anfühlen – die Lektion **Zahlensysteme** im Modul Digital Fundamentals geht das im Detail durch.

Die Zahlentheorie reicht dir außerdem Werkzeuge, die du beim Namen nennen wirst:

- **Teilbarkeit und modulare Arithmetik.** Ein Zähler, der bei K überläuft, rechnet *mod K*; Taktteiler und das Umbrechen von Adressen sind Kongruenzen (Einheit IV, *enteros módulo n*).
- **Vollständige Induktion.** Um zu beweisen, dass ein N-Bit-Ripple-Carry-Addierer korrekt ist, zeigst du, dass der 1-Bit-Fall funktioniert, und dann: „wenn die N-Bit-Version funktioniert, dann auch N+1." Das ist die Gestalt jeder `for … generate`-Schleife, die einen breiten Datenpfad aus einer wiederholten Scheibe zusammensetzt.

## Bits zählen

Zwei Formeln schließen den Kreis zwischen allen vier Einheiten:

- Ein Port der Breite N hält **2^N** verschiedene Werte (kartesisches Produkt / Potenzmenge).
- Eine Maschine mit K Zuständen passt in **⌈log₂ K⌉** Flip-Flops bei binärer Kodierung, oder in **K** Flip-Flops bei One-Hot.

Deshalb passt ein Ampel-Zustandsautomat in 2 Flip-Flops, und deshalb sieht ein 32-Bit-Zähler etwa 4 Milliarden Flanken, bevor er überläuft.

## Was du behalten solltest

- Aussagenlogik *ist* boolesche Algebra – dieselben Operatoren, und P ⇒ Q ist nur `(not P) or Q`. De Morgan lässt dich Negationen durch Gatter schieben.
- Eine boolesche Funktion hat die kanonischen Formen Summe-der-Minterme / Produkt-der-Maxterme; Karnaugh-Diagramme und die Axiome verkleinern sie.
- Mengen benennen die Werte, Ports und Zustände einer Schaltung; **2ⁿ** (kartesisches Produkt / Potenzmenge) und **⌈log₂ K⌉** sind die Breitenformeln, die du für immer wiederverwendest.
- Kombinatorische Logik ist eine *totale, deterministische* Funktion – weise jeden Ausgang auf jedem Pfad zu, sonst bekommst du ein versehentliches Latch.
