## BCD nach 7-Segment — Ziffer 5 ergänzen

Ein BCD-zu-7-Segment-Decoder ist **fast** fertig: Die Ziffern 0-4 und
6-9 sind bereits in einem `case` verdrahtet. Nur die Eingabe für die
Ziffer 5 fehlt.

Schreibe die Zeile, die `digit = "0101"` auf das passende 7-Segment-
Muster abbildet. Reihenfolge ist `seg(6..0) = a..g`, **aktiv hoch**
(brennendes Segment = `'1'`).

Für die Ziffer 5 leuchten `a, c, d, f, g` (`b` und `e` aus):

```
seg = "1011011"
```

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Die anderen
`case`-Zweige sind gesperrt.

### Hinweis

Genau eine Zeile: `when "0101" => seg <= "1011011";`.
