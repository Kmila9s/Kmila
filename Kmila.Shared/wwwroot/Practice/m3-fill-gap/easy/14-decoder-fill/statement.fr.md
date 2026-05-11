## Ligne Y0 du décodeur 2→4 — compléter

Trois des quatre sorties du décodeur sont câblées. Implémente la ligne **Y0** (active quand `sel = "00"`).

```vhdl
y0_int <= '1' when sel = "00" else '0';
```

Seule la ligne entre les marqueurs est évaluée.
