## Volladdierer — Fehler finden und beheben

Der Autor sollte einen 1-Bit-Volladdierer implementieren. Die Ausgabe `sum` stimmt, aber `cout` ist **falsch** für mindestens eine Eingangskombination.

Finde den Fehler in der Architektur und korrigiere ihn.

### Spezifikation

Für `a`, `b`, `cin` ∈ {0, 1}:

* `sum  = a ⊕ b ⊕ cin`
* `cout = (a ∧ b) ∨ (cin ∧ (a ⊕ b))`

### Hinweis

Der Übertrag muss `cin` berücksichtigen — nicht nur `a` und `b`.
