## Syndrome Hamming(7,4) — compléter

Calcule le syndrome 3 bits pour un mot Hamming(7,4) `code(6 downto 0)`. Chaque bit du syndrome est la parité d'un sous-ensemble spécifique — un code valide donne `"000"`.

```vhdl
s(0) <= code(6) xor code(4) xor code(2) xor code(0);
s(1) <= code(5) xor code(4) xor code(1) xor code(0);
s(2) <= code(3) xor code(2) xor code(1) xor code(0);
```

Seules les lignes entre les marqueurs sont évaluées.
