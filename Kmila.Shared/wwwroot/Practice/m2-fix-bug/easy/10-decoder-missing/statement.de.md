## 2-zu-4-Dekoder — Bug finden und beheben

Ein 2-zu-4-Dekoder aktiviert genau **einen** der vier
Ausgangsbits gemäß `sel`. Drei der vier Fälle sind korrekt
abgedeckt, aber einer wurde vergessen und fällt auf den
Default-Wert `"0000"` zurück.

Finde den fehlenden Fall und ergänze ihn.

### Spezifikation

| `sel` | `y`     |
|:-----:|:-------:|
| 00    | 0001    |
| 01    | 0010    |
| 10    | 0100    |
| 11    | 1000    |

### Hinweis

Welcher Wert von `sel` taucht in den `when`-Zweigen nicht auf?
