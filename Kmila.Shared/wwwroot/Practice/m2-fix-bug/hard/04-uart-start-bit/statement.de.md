## UART — repariere das Start-Bit-Timing

Diese minimale UART-Empfangs-FSM hat vier Zustände: `IDLE`, `START`,
`DATA`, `STOP` (codiert `00`, `01`, `10`, `11` an `state`). Im
aktuellen Code verlässt die FSM den Zustand `START` mitten im
Bit-Zeitraum, und der Rest des Frames wird falsch abgetastet.

### Spezifikation

* `clk` mit 50 MHz, `BIT_PERIOD = 10` Takte pro Bit.
* Aus `IDLE`: eine fallende Flanke auf `rx` (`rx = '0'`) bringt die
  FSM sofort nach `START`, **am Anfang des Bit-Zeitraums**.
* `START` und `DATA` dauern genau `BIT_PERIOD` Takte.
* `STOP` dauert einen Takt und kehrt zu `IDLE` zurück.

### Hinweis

Wenn die Bit-Periodenkonstante halb so groß ist wie sie sein
sollte, tastet die FSM die Mitte jedes Bits ab statt den Anfang.
Die Konstante neu aus Takt und Baud-Rate ableiten.
