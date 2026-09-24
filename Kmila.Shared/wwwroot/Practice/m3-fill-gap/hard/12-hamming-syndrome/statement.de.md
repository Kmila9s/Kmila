## Hamming(7,4)-Syndrom — füllen

Berechne das 3-Bit-Syndrom eines Hamming(7,4)-Codewortes `code(6 downto 0)`. Jedes Syndrom-Bit ist die Parität einer spezifischen Teilmenge — ein gültiges Codewort liefert `"000"`.

```vhdl
s(0) <= code(6) xor code(4) xor code(2) xor code(0);
s(1) <= code(5) xor code(4) xor code(1) xor code(0);
s(2) <= code(3) xor code(2) xor code(1) xor code(0);
```

Nur die Zeilen zwischen den Markierungen werden bewertet.
