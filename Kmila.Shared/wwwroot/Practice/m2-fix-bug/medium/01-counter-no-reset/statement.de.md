## 4-Bit-Zähler — Bug im asynchronen Reset

Der Block soll ein 4-Bit-Zähler mit **asynchronem, high-aktivem Reset** sein.

### Spezifikation

* Bei `rising_edge(clk)`: `count <- count + 1`.
* Wenn `reset = '1'`: `count` wird sofort auf `"0000"` gezwungen, unabhängig vom Takt.
* `q` spiegelt `count` wider.

### Was schiefläuft

Wenn du `reset` zwischen zwei Taktflanken pulsst, wird der Zähler **nicht** sofort gelöscht — erst bei der nächsten Flanke. Der Process-Körper ist korrekt; der Fehler liegt eine Zeile darüber.

### Hinweis

Für einen wirklich asynchronen Reset muss das Reset-Signal in der **Sensitivity-Liste** des Process neben dem Takt stehen.
