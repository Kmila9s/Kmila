## FIFO — repariere den Pointer-Wrap

Ein FIFO der Tiefe 4 verliert Daten, sobald der Puffer überläuft.
Die `count`/`full`/`empty`-Logik sieht korrekt aus, aber die Lese-
und Schreib-Pointer wickeln einen Slot zu spät, sodass das FIFO
sich wie ein Fünfer-FIFO verhält statt wie ein Vierer.

### Spezifikation

* Synchrones FIFO der Tiefe 4, high-aktiver Reset.
* Pointer nehmen die Werte `0..3` und **müssen nach 3 zurück auf
  0 springen**.
* `full = '1'` bei `count = 4`, `empty = '1'` bei `count = 0`.
* Push wird bei voller, Pop bei leerer FIFO ignoriert.

### Hinweis

Bei `DEPTH = 4` ist der zulässige Pointer-Bereich `0..DEPTH-1`.
Ein Wrap bei Gleichheit mit `DEPTH` (oder gar `DEPTH+1`) erzeugt
einen Geister-Slot.
