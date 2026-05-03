## Divisor de reloj entre 4

Genera una señal de salida con frecuencia un cuarto de la entrada. La forma más sencilla: contar flancos ascendentes y emitir el bit alto de un contador de 2 bits — alterna cada 4 ciclos de entrada.

### Especificación

- Interno: `signal cnt : unsigned(1 downto 0) := (others => '0');`.
- En cada flanco: `cnt <= cnt + 1;`.
- Salida: `clk_out <= cnt(1);`.

### Pista

`cnt(1)` cambia tras `00 → 01 → 10 → 11`, dando `clk/4`.
