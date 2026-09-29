# Labor: Wahrheitstabellen-Explorer

Dieses Labor ist das erste Mal, dass du ein echtes VHDL-Design treibst. Die Entity hat zwei Eingänge (`a`, `b`) und sechs Ausgänge — einen pro elementarem 2-Eingang-Gatter.

## Was passiert, wenn du *Im Editor öffnen* klickst

1. Kmila erstellt ein Wegwerf-Projekt namens „Labor: Wahrheitstabellen-Explorer“.
2. Er füllt es mit `gates.vhd`, das die sechs nebenläufigen Zuweisungen enthält.
3. Er lädt eine **Simulationskonfiguration**, die `a` und `b` durch jede Kombination stimuliert.
4. Die Editor-Werkbank öffnet sich, mit der Config oben im Pill schon ausgewählt.

Von dort:

- Klick **Build**, um das Design zu parsen und zu elaborieren.
- Klick **Run**, um zu simulieren.
- Wechsle im Dock unten zum **Wellenformen**-Tab.

Du siehst sieben Zeilen Rechteckwellen: deine zwei Eingänge oben und jeden Ausgang in Echtzeit reagierend.

## Worauf achten

Nutze den Scrollbalken oder die Tastatur, um die Eingangsflanken mit den Ausgangsübergängen abzugleichen. Versuche, das Verhalten jedes Gatters zu verifizieren, ohne nochmal die Wahrheitstabellen anzuschauen:

- `o_and`: bleibt low, bis sowohl `a` als auch `b` 1 sind — dann poppt es high.
- `o_nand`: das Gegenteil. Es ist normalerweise high und geht nur low, wenn beide Eingänge 1 sind.
- `o_xor`: high genau dann, wenn die zwei Eingänge widersprechen.

Falls eines nicht zu deiner Erwartung passt, öffne den **Output**-Tab — Build-Warnungen landen dort.

## Probier es selbst

Sobald das Beispiel läuft, experimentiere:

- Füge der Entity einen dritten Eingang `c` hinzu und berechne `a xor b xor c` (3-Eingang-Parität).
- Benenne einen Ausgang um und baue neu — der **Ports**-Tab aktualisiert sich passend.
- Füge der Config eine weitere Zeile hinzu: flippe `a` und `b` gleichzeitig und sieh, welche Ausgänge sich ändern.

Je mehr du diese Hebel bewegst, bevor du dein eigenes Design schreibst, desto weniger fühlt sich VHDL wie Raten an.
