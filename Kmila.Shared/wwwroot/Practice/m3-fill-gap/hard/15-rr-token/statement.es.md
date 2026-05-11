## Token round-robin — completa

Un token one-hot de 4 bits rota a la izquierda una posición por ciclo. El reset deja el token en `"0001"`. Implementa la línea de rotación.

```vhdl
tok <= tok(2 downto 0) & tok(3);
```

Sólo se evalúa la línea entre los marcadores.
