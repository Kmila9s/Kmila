## T-Flipflop — Enable-Eingang ignoriert

Verlangt war ein T-Flipflop mit **Toggle-Enable** `t`.

### Spezifikation

* Asynchroner High-aktiver `reset` setzt `q` auf `'0'`.
* Bei `rising_edge(clk)`:
  * Wenn `t = '1'`: `q <- not q`.
  * Wenn `t = '0'`: `q` bleibt unverändert.

### Was schiefläuft

Der Code toggelt `q` bei jeder steigenden Flanke, unabhängig von `t`. Das Verhalten entspricht einem Taktteiler, nicht einem steuerbaren T-Flipflop.

### Hinweis

Die Toggle-Zuweisung muss innerhalb eines `if t = '1' then ...` im Rising-Edge-Zweig stehen.
