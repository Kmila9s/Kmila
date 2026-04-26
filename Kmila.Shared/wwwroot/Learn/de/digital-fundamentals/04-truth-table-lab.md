# Lab: Wahrheitstabellen-Explorer

Dieses Lab ist das erste Mal, dass du ein echtes VHDL-Design ansteuerst. Die Entity hat zwei Eingänge (`a`, `b`) und sechs Ausgänge — einen pro elementarem 2-Eingangs-Gatter.

## Was passiert, wenn du *Im Editor öffnen* klickst

1. Kmila erstellt ein Wegwerf-Projekt namens etwa "Lab: Wahrheitstabellen-Explorer".
2. Es befüllt es mit `gates.vhd`, das die sechs nebenläufigen Zuweisungen enthält.
3. Es lädt eine **Simulationskonfiguration**, die `a` und `b` durch jede Kombination stimuliert.
4. Die Editor-Arbeitsfläche öffnet sich, mit der Konfiguration bereits in der Pille oben ausgewählt.

Von dort aus:

- Klicke auf **Build**, um das Design zu parsen und zu elaborieren.
- Klicke auf **Run**, um zu simulieren.
- Wechsle zum **Waveforms**-Tab im Dock unten.

Du wirst sieben Zeilen Rechteckwellen sehen: deine zwei Eingänge oben und jeden Ausgang, der in Echtzeit reagiert.

## Worauf achten

Verwende die Bildlaufleiste oder die Tastatur, um die Eingangsflanken mit den Ausgangsübergängen auszurichten. Versuche, das Verhalten jedes Gatters zu verifizieren, ohne auf die Wahrheitstabellen zurückzuschauen:

- `o_and`: bleibt niedrig, bis sowohl `a` als auch `b` 1 sind — dann springt es hoch.
- `o_nand`: das Gegenteil. Es ist normalerweise hoch und geht nur dann auf low, wenn beide Eingänge 1 sind.
- `o_xor`: hoch genau dann, wenn sich die beiden Eingänge unterscheiden.

Wenn einer davon nicht dem entspricht, was du erwartest, öffne den **Output**-Tab — Build-Warnungen landen dort.

## Probier es selbst

Sobald das Beispiel funktioniert, experimentiere:

- Füge einen dritten Eingang `c` zur Entity hinzu und berechne `a xor b xor c` (Drei-Eingangs-Parität).
- Benenne einen Ausgang um und baue neu — der **Ports**-Tab wird entsprechend aktualisiert.
- Füge eine weitere Zeile zur Konfiguration hinzu: Kippe `a` und `b` gleichzeitig und sieh, welche Ausgänge sich ändern.

Je mehr du diese Hebel vor dem Schreiben deines eigenen Designs bewegst, desto weniger fühlt sich VHDL wie Raten an.
