## Multiplicateur 4 bits non signé

Multiplicateur combinatoire : `product = a * b`, avec `a` et `b` 4 bits et `product` 8 bits.

### Spécification

- Pur combinatoire — pas d'horloge.
- Utilise `IEEE.NUMERIC_STD`. L'opérateur `*` est surchargé pour `unsigned`.
- La largeur naturelle de `unsigned(3 downto 0) * unsigned(3 downto 0)` est exactement 8 bits.

### Indice

Une ligne :

```vhdl
product <= std_logic_vector(unsigned(a) * unsigned(b));
```
