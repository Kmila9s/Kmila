## 4-zu-2-Prioritätsencoder — Bug finden und beheben

Ein 4-zu-2-Prioritätsencoder gibt den **Index des höchsten
gesetzten Bits** von `d` aus. Der Autor hat eine Kette von
`when`-Klauseln geschrieben, die die Bits in der **falschen
Reihenfolge** durchläuft. Sind mehrere Bits `1`, liefert der
Encoder den niedrigsten Index statt des höchsten.

Finde den Fehler und korrigiere die Prioritätsreihenfolge.

### Spezifikation

* Wenn `d(3) = 1` → `y = "11"`
* Sonst wenn `d(2) = 1` → `y = "10"`
* Sonst wenn `d(1) = 1` → `y = "01"`
* Sonst → `y = "00"`

### Hinweis

`when ... else` wird **von oben nach unten** ausgewertet — die
erste passende Klausel gewinnt.
