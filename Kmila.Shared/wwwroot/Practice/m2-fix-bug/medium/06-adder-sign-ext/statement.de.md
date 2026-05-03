## Addierer mit gemischten Breiten — Vorzeichenerweiterung-Bug

`a` ist eine 4-Bit-Zweierkomplement-Zahl (Bereich −8…+7), `b` ist 8 Bit, ebenfalls Zweierkomplement. Die Ausgabe `sum = a + b` ist 8 Bit.

### Spezifikation

* Erweitere `a` per **Vorzeichenerweiterung** von 4 auf 8 Bit, dann addiere zu `b`.
* Repliziere das Vorzeichenbit `a(3)` in die oberen vier Bits.

### Was schiefläuft

Der Autor erweitert `a` einfach mit `"0000"` davor — das ist **Null-Erweiterung**. Für positive `a` klappt es zufällig, aber für negatives `a` (z. B. `1111` = −1) entsteht +15 statt −1 und die Summe ist falsch.

### Hinweis

`numeric_std` bietet `resize(signed_wert, neue_breite)`, das bei `signed`-Quellen automatisch eine Vorzeichenerweiterung durchführt.
