## Flip-flop T

Implementa un flip-flop tipo T (toggle). En cada flanco ascendente, si `t = '1'`, la salida `q` se invierte. Si `t = '0'`, `q` se mantiene. Un `reset` asíncrono activo en alto pone `q` a `0`.

### Especificación

- Interno: `signal qi : std_logic := '0';`.
- Cuerpo del proceso: `if t = '1' then qi <= not qi;`.
- Salida: `q <= qi;`.

### Pista

Maneja `q` desde una señal interna `qi` porque las salidas en VHDL no se pueden leer dentro de un proceso.
