## Code Gray 3 bits

Convertis une entrée binaire 3 bits `b` en code Gray `g`. En Gray, un seul bit change entre deux valeurs consécutives.

### Équations

- `g(2) = b(2)`
- `g(1) = b(2) XOR b(1)`
- `g(0) = b(1) XOR b(0)`

### Table de vérité

| `b` | `g` |
|:---:|:---:|
| 000 | 000 |
| 001 | 001 |
| 010 | 011 |
| 011 | 010 |
| 100 | 110 |
| 101 | 111 |
| 110 | 101 |
| 111 | 100 |
