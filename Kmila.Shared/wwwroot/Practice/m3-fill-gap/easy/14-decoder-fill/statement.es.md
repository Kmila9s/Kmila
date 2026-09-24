## Línea Y0 del decodificador 2→4 — completa

Tres de las cuatro salidas del decodificador están cableadas. Implementa la línea **Y0** (se activa cuando `sel = "00"`).

```vhdl
y0_int <= '1' when sel = "00" else '0';
```

Sólo se evalúa la línea entre los marcadores.
