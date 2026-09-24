## FSM de semáforo

Implementa un controlador de semáforo de 3 estados. En cada flanco ascendente avanza por el ciclo `"00"` (rojo) → `"01"` (verde) → `"10"` (amarillo) → de vuelta a `"00"`.

### Especificación

- Reset asíncrono: `if reset='1'` lleva `state` a `"00"`.
- Un paso por reloj; sin divisores — el banco de pruebas avanza la FSM en cada flanco.
- `state` es un `STD_LOGIC_VECTOR` de 2 bits.

### Pista

Guarda el estado en un `unsigned(1 downto 0)` y selecciona el siguiente valor con `if/elsif`.
