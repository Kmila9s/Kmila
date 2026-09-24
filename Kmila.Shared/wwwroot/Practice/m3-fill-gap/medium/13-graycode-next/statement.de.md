## Binär → Gray — Umrechnung füllen

Ein 3-Bit-Binärzähler `b(2..0)` läuft bereits und wird per Reset
zurückgesetzt. Der Ausgangsbus `g` wird vom Gray-Vektor `gi`
getrieben. Berechne `gi` aus `b`.

### Spezifikation

Die Standard-Umrechnung Binär → Gray lautet:

```
gi(2) = b(2)
gi(1) = b(2) xor b(1)
gi(0) = b(1) xor b(0)
```

Sie erzeugt die Folge `000, 001, 011, 010, 110, 111, 101, 100, …`,
in der sich pro Schritt nur ein Bit ändert.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Zähler,
Reset und `g <= gi` sind gesperrt.

### Hinweis

Drei nebenläufige Zuweisungen an die Bits von `gi`, oder eine
zusammengefasste: `b(2) & (b(2) xor b(1)) & (b(1) xor b(0))`.
