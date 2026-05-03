## Codeur Hamming(7,4)

Encode un mot 4 bits `d = d3 d2 d1 d0` en mot Hamming 7 bits en ajoutant trois bits de parité.

### Équations

| Bit | Formule                |
|:---:|:-----------------------|
| `p1`| `d0 xor d1 xor d3`     |
| `p2`| `d0 xor d2 xor d3`     |
| `p3`| `d1 xor d2 xor d3`     |

### Disposition

```
code <= p1 & p2 & d0 & p3 & d1 & d2 & d3;
```

### Spécification

Combinatoire uniquement. Utilise des signaux intermédiaires pour la parité.

### Indice

Chaque parité couvre un sous-ensemble de `d` — c'est la matrice génératrice Hamming standard.
