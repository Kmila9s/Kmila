## Registro de desplazamiento de 8 bits

Construye un registro de desplazamiento de 8 bits. En cada flanco ascendente del reloj, desplaza el contenido a la izquierda una posición e introduce la entrada serial `din` en el bit 0. Un `reset` asíncrono activo en alto limpia el registro.

### Especificación

- Señal interna: `r : std_logic_vector(7 downto 0)`.
- Regla de actualización: `r <= r(6 downto 0) & din;`.
- Salida: `q <= r;`.

### Pista

Usa un `process(clk, reset)` con `if reset = '1' then ... elsif rising_edge(clk) then ...`.
