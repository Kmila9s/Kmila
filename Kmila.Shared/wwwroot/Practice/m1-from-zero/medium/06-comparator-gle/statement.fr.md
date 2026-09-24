## Comparateur >, =, <

Compare deux valeurs non signées 4 bits `a` et `b` et pilote trois drapeaux :

- `gt = 1` si `a > b`
- `eq = 1` si `a = b`
- `lt = 1` si `a < b`

Exactement un drapeau vaut `1` à chaque instant.

### Indice

Utilise `IEEE.NUMERIC_STD.ALL` et la forme concurrente conditionnelle :

```vhdl
gt <= '1' when unsigned(a) > unsigned(b) else '0';
```
