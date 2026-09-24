## 2-zu-1-MUX — Bug finden und beheben

Ein 2-zu-1-Multiplexer soll `a` an `y` weiterleiten, wenn
`sel = 0`, und `b` an `y`, wenn `sel = 1`. Der Autor hat die
Polarität von `sel` **vertauscht**.

Finde den Fehler und korrigiere ihn.

### Spezifikation

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Hinweis

Achte auf den Wert von `sel` in der `when`-Klausel.
