## 8-Bit-LFSR

Implementiere ein 8-Bit-LFSR maximaler Länge mit Taps an 8, 6, 5, 4 (Fibonacci-Form).

### Spezifikation

- Internes Register `r : STD_LOGIC_VECTOR(7 downto 0)`, Saat `"11111111"` bei Reset (Null verboten — Selbstblockade).
- Pro steigender Flanke:
  - `fb := r(7) xor r(5) xor r(4) xor r(3);`
  - `r <= r(6 downto 0) & fb;`
- `q <= r`.

### Hinweis

Eine `variable fb` im Prozess hält die Formel klar und synthetisierbar.
