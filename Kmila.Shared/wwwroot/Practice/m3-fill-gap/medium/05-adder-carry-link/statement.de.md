## 2-Bit-Ripple-Addierer — Übertragskette füllen

Zwei Volladdierer sind teilweise geschrieben. Die Summenbits und der
finale Übertrag sind bereits vorhanden; nur der **interne Übertrag**
`c1` zwischen Bit-0 und Bit-1 fehlt.

Du musst `c1` mit dem Übertragsausgang des Bit-0-Volladdierers
versorgen:

```
c1 = (a0·b0) + cin·(a0 ⊕ b0)
```

### Spezifikation

* Eingänge: `a`, `b` (je 2 Bit) und `cin`.
* Ausgänge: `sum` (2 Bit) und `cout`.
* Die anderen Zuweisungen sind bereits gegeben.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren.

### Hinweis

`c1 <= (a(0) and b(0)) or (cin and (a(0) xor b(0)));`
