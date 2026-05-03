## 4-Bit-Gleichheit mit XOR — Bug finden und beheben

Diese Schaltung soll `eq = 1` ausgeben, wenn `a = b`, sonst `0`.
Der Autor hat es mit einer langen `xor`-Kette über die einzelnen
Bits versucht — aber das ergibt die **Parität**, nicht die
Gleichheit.

Finde den Fehler und nimm den richtigen Vergleich.

### Spezifikation

* `eq = 1` wenn `a = b`, sonst `0`.

### Hinweis

Gleichheit gilt, wenn **jedes** Bitpaar übereinstimmt — nicht
wenn die Anzahl der Einsen gerade oder ungerade ist.
