## LFSR rétroaction — compléter

Implémente le calcul de rétroaction et le décalage pour un LFSR 8 bits de longueur maximale avec taps aux positions 8, 6, 5, 4.

```vhdl
fb := r(7) xor r(5) xor r(4) xor r(3);
r <= r(6 downto 0) & fb;
```

Le registre démarre à `"11111111"` après reset. Seules les lignes entre les marqueurs sont évaluées.
