## Additionneur complet

Implémente un additionneur complet 1 bit. Il additionne trois bits — `a`, `b` et la retenue entrante `cin` — et produit `sum` et la retenue sortante `cout`.

### Équations

- `sum  = a XOR b XOR cin`
- `cout = (a AND b) OR (cin AND (a XOR b))`

### Indice

Le XOR des trois entrées donne la somme. La retenue de sortie apparaît dès qu'au moins deux entrées valent `1`.
