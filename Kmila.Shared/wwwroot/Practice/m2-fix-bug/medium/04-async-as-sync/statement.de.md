## D-Flipflop — asynchroner Reset, der keiner ist

Verlangt war ein D-Flipflop mit **asynchronem, high-aktivem Reset**.

### Spezifikation

* Bei `rising_edge(clk)`: `q <- d`.
* Wenn `reset = '1'`: `q` wird sofort auf `'0'` gezwungen, ohne Takt.

### Was schiefläuft

Wenn `reset` zwischen Taktflanken aktiviert wird, wird `q` nicht sofort gelöscht — der Simulator wertet den Process nur bei einem Wechsel von `clk` neu aus. Der Reset ist effektiv **synchron** geworden, was die Spec verletzt.

### Hinweis

Die Sensitivity-Liste steuert, wann ein Process neu ausgewertet wird. Ein asynchroner Reset muss neben `clk` darin stehen.
