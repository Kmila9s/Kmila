## Volladdierer

Implementiere einen 1-Bit-Volladdierer. Er addiert drei Bits — `a`, `b` und den Eingangsübertrag `cin` — und liefert `sum` sowie den Ausgangsübertrag `cout`.

### Gleichungen

- `sum  = a XOR b XOR cin`
- `cout = (a AND b) OR (cin AND (a XOR b))`

### Hinweis

Das XOR aller drei Eingänge ergibt die Summe. Der Ausgangsübertrag entsteht, sobald mindestens zwei Eingänge `1` sind.
