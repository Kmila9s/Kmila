## Contador síncrono de 4 bits

Construye un contador de 4 bits que incrementa en cada flanco ascendente de reloj. Un `reset` asíncrono activo en alto pone la cuenta a `0000`.

### Especificación

- Interno: `count : unsigned(3 downto 0) := (others => '0')`.
- En `rising_edge(clk)`: `count <= count + 1;`.
- Salida: `q <= std_logic_vector(count);`.

### Pista

Usa `IEEE.NUMERIC_STD.ALL` para que `+ 1` funcione sobre `unsigned`.
