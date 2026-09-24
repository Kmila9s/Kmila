## Contador 4 bits asc./desc.

Implementa un contador de 4 bits con control de dirección. En cada flanco ascendente:

- si `dir = '1'`, suma: `count <= count + 1`
- si `dir = '0'`, resta: `count <= count - 1`

Un `reset` asíncrono activo en alto pone el contador a `0000`.

### Pista

Usa `IEEE.NUMERIC_STD.ALL` y una señal interna `unsigned(3 downto 0)`. La cuenta da la vuelta módulo 16.
