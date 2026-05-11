## Síndrome Hamming(7,4) — completa

Calcula el síndrome de 3 bits para una palabra Hamming(7,4) `code(6 downto 0)`. Cada bit del síndrome es la paridad de un subconjunto específico — el código válido produce `"000"`.

```vhdl
s(0) <= code(6) xor code(4) xor code(2) xor code(0);
s(1) <= code(5) xor code(4) xor code(1) xor code(0);
s(2) <= code(3) xor code(2) xor code(1) xor code(0);
```

Sólo se evalúan las líneas entre los marcadores.
