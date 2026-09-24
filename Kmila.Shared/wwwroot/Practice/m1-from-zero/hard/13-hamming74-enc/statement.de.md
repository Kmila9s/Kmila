## Hamming(7,4)-Codierer

Codiere ein 4-Bit-Datenwort `d = d3 d2 d1 d0` in ein 7-Bit-Hamming-Codewort durch Anhängen dreier Paritätsbits.

### Paritätsgleichungen

| Bit | Formel                 |
|:---:|:-----------------------|
| `p1`| `d0 xor d1 xor d3`     |
| `p2`| `d0 xor d2 xor d3`     |
| `p3`| `d1 xor d2 xor d3`     |

### Ausgabeformat

```
code <= p1 & p2 & d0 & p3 & d1 & d2 & d3;
```

### Spezifikation

Nur kombinatorisch. Zwischensignale für die Parität verwenden.

### Hinweis

Jedes Paritätsbit deckt eine bestimmte Teilmenge von `d` ab — Standard-Hamming-Generatormatrix.
