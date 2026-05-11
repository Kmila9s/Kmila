## Jeton round-robin — compléter

Un jeton one-hot 4 bits tourne à gauche d'une position chaque cycle. Le reset remet le jeton à `"0001"`. Implémente la ligne de rotation.

```vhdl
tok <= tok(2 downto 0) & tok(3);
```

Seule la ligne entre les marqueurs est évaluée.
