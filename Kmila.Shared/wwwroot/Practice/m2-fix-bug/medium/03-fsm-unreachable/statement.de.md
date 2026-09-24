## 3-Zustands-FSM — nicht erreichbarer Zustand

Verlangt war ein Automat mit `S0` (`"00"`), `S1` (`"01"`), `S2` (`"10"`).

### Spezifikation

* Asynchroner High-aktiver Reset → `S0`.
* Bei `rising_edge(clk)`:
  * `S0 -> S1` immer.
  * `S1 -> S2`, wenn `x = '1'`, sonst bleibt in `S1`.
  * `S2 -> S0` immer.
* `state` gibt die Zustandskodierung aus.

### Was schiefläuft

Der Übergang `S1 -> S2` feuert nie. Egal wie lange `x` high gehalten wird, die FSM bleibt für immer in `S1` — `S2` ist **nicht erreichbar**. Der Fehler steckt in der Bedingung dieses Übergangs.

### Hinweis

Ein Signal kann nicht gleichzeitig zwei verschiedene Werte haben. Sieh dir den Booleschen Ausdruck an — er ist ein Widerspruch.
