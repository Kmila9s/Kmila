## Registro con load (4 bits)

Implementa un registro de 4 bits con carga síncrona. En un flanco ascendente, si `load = '1'`, copia `d` al registro. Si no, mantiene el valor anterior. Un `reset` asíncrono activo en alto lo pone a `0000`.

### Especificación

- Interno: `signal r : std_logic_vector(3 downto 0) := (others => '0');`.
- Proceso: `if load = '1' then r <= d;`.
- Salida: `q <= r;`.

### Pista

Un bloque común — misma estructura que un contador, pero el nuevo valor viene de `d` y no de `count + 1`.
