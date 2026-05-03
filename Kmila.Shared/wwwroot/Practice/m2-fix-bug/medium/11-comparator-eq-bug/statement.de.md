## 4-Bit-Komparator — Gleichheitsausgang hängt

Verlangt war ein vorzeichenloser 4-Bit-Komparator mit drei Flags:

* `gt = '1'`, wenn `a > b`.
* `eq = '1'`, wenn `a = b`.
* `lt = '1'`, wenn `a < b`.

### Was schiefläuft

`gt` und `lt` sind korrekt, aber `eq` ist fest auf `'0'` verdrahtet. Bei gleichen Eingängen (z. B. `a = b = 0101`) sind **alle drei** Flags low — ein inkonsistenter Komparatorzustand.

### Hinweis

Ersetze die konstante Zuweisung an `eq` durch eine bedingte Zuweisung wie bei `gt` und `lt`, aber mit einem Gleichheitstest.
