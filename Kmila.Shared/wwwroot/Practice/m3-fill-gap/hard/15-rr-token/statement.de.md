## Round-Robin-Token — füllen

Ein 4-Bit-One-Hot-Token rotiert pro Takt um eine Position nach links. Reset setzt das Token zurück auf `"0001"`. Implementiere die Rotationszeile.

```vhdl
tok <= tok(2 downto 0) & tok(3);
```

Nur die Zeile zwischen den Markierungen wird bewertet.
