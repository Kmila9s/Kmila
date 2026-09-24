## LFSR-Rückkopplung — füllen

Implementiere die Rückkopplung und den Shift für ein 8-Bit-LFSR maximaler Länge mit Taps an den Positionen 8, 6, 5, 4.

```vhdl
fb := r(7) xor r(5) xor r(4) xor r(3);
r <= r(6 downto 0) & fb;
```

Das Register startet nach Reset bei `"11111111"`. Nur die Zeilen zwischen den Markierungen werden bewertet.
