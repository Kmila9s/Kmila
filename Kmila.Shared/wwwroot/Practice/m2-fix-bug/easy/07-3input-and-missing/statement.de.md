## 3-Eingang-UND — Bug finden und beheben

Der Autor sollte ein UND-Gatter mit 3 Eingängen schreiben, aber
der aktuelle Code verundet nur `a` und `b`. Der dritte Eingang
`c` wird ignoriert.

Finde den Fehler und nimm `c` wieder in den Ausdruck auf.

### Spezifikation

* `y = a ∧ b ∧ c`
* `y = 1` nur, wenn alle drei Eingänge `1` sind.

### Hinweis

Auf der rechten Seite von `<=` fehlt ein Operand.
