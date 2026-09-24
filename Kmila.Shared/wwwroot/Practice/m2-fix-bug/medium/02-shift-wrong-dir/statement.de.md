## 8-Bit-Schieberegister — falsche Richtung

Verlangt war ein Serie-in / Parallel-out-Schieberegister, das pro Takt **nach links** schiebt.

### Spezifikation

* Bei `rising_edge(clk)`: `r <- r(6 downto 0) & din` — `din` kommt am Bit 0 herein, der MSB fällt heraus.
* Asynchroner High-aktiver `reset` setzt `r` auf null.
* `q` spiegelt `r`.

### Was schiefläuft

Die Konkatenation ist verdreht: `din` wird am MSB-Ende eingespeist, sodass nach einigen Zyklen mit `din = '1'` zuerst die oberen Bits gefüllt werden statt der unteren.

### Hinweis

In `r <= a & b` wird der **linke** Operand zum MSB des Ergebnisses. Für einen Linksshift verwirf den MSB und stelle `din` rechts ein.
