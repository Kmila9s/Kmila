## Realimentación LFSR — completa

Implementa el cálculo de la realimentación y el desplazamiento para un LFSR de longitud máxima de 8 bits con taps en las posiciones 8, 6, 5, 4.

```vhdl
fb := r(7) xor r(5) xor r(4) xor r(3);
r <= r(6 downto 0) & fb;
```

El registro arranca con `"11111111"` tras reset. Sólo se evalúan las líneas entre los marcadores.
