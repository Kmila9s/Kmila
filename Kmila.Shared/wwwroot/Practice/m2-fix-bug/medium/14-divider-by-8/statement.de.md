## Taktteiler — falscher Teiler

Verlangt war ein Teiler durch 4.

### Spezifikation

* Bei jeder `rising_edge(clk)` wird ein 3-Bit-Zähler `cnt` inkrementiert.
* `clk_out` ist **Bit 1** des Zählers — `clk_out` toggelt alle 2 Eingangstakte → Ausgangsperiode = 4 Eingangstakte → Teilung durch 4.
* Asynchroner High-aktiver `reset` setzt `cnt` auf null.

### Was schiefläuft

Der Starter greift `cnt(2)` statt `cnt(1)` ab. Das teilt durch **8**, nicht durch 4 — `clk_out` toggelt erst alle 4 Eingangstakte.

### Hinweis

Bei einem Zweierpotenz-Teiler aus einem freilaufenden Zähler teilt das Bit `n` durch `2^(n+1)`. Wähle das Bit, das eine Periode von 4 ergibt.
