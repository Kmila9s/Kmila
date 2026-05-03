## Additionneur 4 bits

Implémente un additionneur 4 bits avec retenue d'entrée `cin`. Calcule `sum = a + b + cin` et la retenue sortante `cout`.

### Spécification

- Entrées : `a` et `b` sont des `STD_LOGIC_VECTOR` 4 bits ; `cin` est un bit.
- Sorties : `sum` sur 4 bits ; `cout` sur 1 bit.

### Indice

Utilise `IEEE.NUMERIC_STD.ALL` et un signal temporaire `unsigned` de 5 bits :

```vhdl
signal s : unsigned(4 downto 0);
s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
sum  <= std_logic_vector(s(3 downto 0));
cout <= s(4);
```
