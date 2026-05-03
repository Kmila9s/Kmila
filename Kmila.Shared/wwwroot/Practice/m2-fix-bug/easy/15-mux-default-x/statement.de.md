## 2-zu-1-MUX — falscher Default-Zweig

Ein 2-zu-1-MUX soll `a` ausgeben, wenn `sel = 0`, und `b`, wenn
`sel = 1`. Der Starter leitet `a` bei `sel = 0` korrekt durch,
aber bei `sel = 1` treibt er `'1'` statt `b` — `b` wird nie
gelesen.

Finde den Fehler und verwende `b` im `else`-Zweig.

### Spezifikation

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Hinweis

Schau dir den Literal nach `else` an — dort sollte ein **Signal**
stehen, keine Konstante.
