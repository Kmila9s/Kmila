## Registro toggle con enable

Construye un registro de 1 bit que alterna. Cuando `en = '1'` en flanco ascendente, la salida `q` se invierte. Si `en = '0'`, `q` no cambia. Un `reset` asíncrono activo en alto pone `q` a `0`.

### Especificación

- Interno: `signal qi : std_logic := '0';`.
- Proceso: `if en = '1' then qi <= not qi;`.
- Salida: `q <= qi;`.

### Pista

Casi idéntico a un flip-flop T. El enable cumple el papel del toggle.
