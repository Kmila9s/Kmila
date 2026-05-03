## Manchester-Codierer

Codiere einen seriellen Eingang `din`, sodass jede Bitperiode eine Flanke enthält (0→1 für `din=0`, 1→0 für `din=1`).

### Spezifikation

- Internes `phase` toggelt pro Takt.
- Reset setzt `phase` auf `'0'`.
- Ausgabe: `m <= din xor phase;`
- Ergebnis: bei `din=0` ergibt `m` `0,1,0,1,...` → Muster `'01'`. Bei `din=1` Muster `'10'`.

### Hinweis

Ein togglendes Flipflop und ein xor — einfachste Leitungscodierung.
