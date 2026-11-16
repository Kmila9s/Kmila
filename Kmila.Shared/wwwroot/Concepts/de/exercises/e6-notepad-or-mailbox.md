**Dein Ziel:** Bei `a = 3` und `b = 5` soll `result` am Ende des Prozesses **16** sein. Das Startprogramm sieht richtig aus — `s <= a + b; result <= s * 2` —, aber drücke **Ausführen** und sieh, was `result` wirklich bekommt.

`s` ist ein **Signal** (ein Briefkasten): Sein neuer Wert kommt erst am Delta-Ende an, also liest die zweite Zeile den alten. Ändere das Programm so, dass der Zwischenwert in der **Variablen** `tmp` (einem Notizblock) liegt, die sich sofort ändert.
