# Proyecto: transmisor UART

Un UART (Universal Asynchronous Receiver/Transmitter) es el primer protocolo real que construye la mayoría de los diseñadores embebidos. Es punto a punto, full duplex, y necesita solo tres cables (TX, RX, GND). Todo adaptador USB-serial FTDI, todo `Serial.print` de Arduino, toda terminal vieja es un UART por dentro.

Este proyecto construye el lado de **transmisión**. El receptor es un excelente próximo proyecto después.

## Formato de trama (sin paridad, 8 bits de datos, 1 stop)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    la línea normalmente está alta ('1'); start es un flanco de bajada.
```

Cada bit se sostiene en `tx` por exactamente `clk_freq / baud` ciclos. Para un reloj de 50 MHz y 115 200 baudios, ≈ 434 ciclos por bit.

## Tu trabajo

1. Construye un contador de reloj que produzca un `baud_tick` de un ciclo cada ~434 ciclos.
2. Al pulsarse `send = '1'`, captura `data` en un registro de desplazamiento de 10 bits: `{ stop, data[7..0], start } = 1 & data & 0`. Levanta `busy <= '1'`.
3. En cada `baud_tick`, desplaza el registro a la derecha (así el start sale primero, luego LSB..MSB de datos, luego stop).
4. Tras el décimo tick, baja `busy` y deja `tx` en alto idle.

## Andamiaje

La entidad y el estado idle están dados; `tx` por defecto es alto, `busy` bajo. Todo lo que hay entre estos defaults es tu responsabilidad.

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## Estructura sugerida

Dos contadores + un registro de desplazamiento:

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- Al detectar `send = '1'` y `sending = '0'`:
  - `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- Mientras `sending = '1'`:
  - `clk_count <= clk_count + 1;`
  - si `clk_count = BAUD_PERIOD - 1`:
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — desplaza a la derecha, relleno alto
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - si `bit_count = 10`:
    - `sending <= '0';`

Haz `BAUD_PERIOD` un generic para encogerlo en simulación (p. ej. 5 ciclos/bit) y que la misma lógica funcione a velocidad real.

## Qué probar

Maneja `send = '1'` con `data = "01001000"` (ASCII `'H'`). Tras completarse la trama, la forma de onda de `tx` debe mostrar 10 períodos de bit:

1. Start (bajo)
2. Bit 0 de datos (LSB: `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Stop (alto)

Y luego idle alto. Haz zoom y verifica bit por bit.

## Más lejos

- Agrega un **FIFO** para encolar varios bytes; `send` se vuelve "push".
- Construye el lado **RX**: muestrea cada bit entrante a 1.5× bit-period y luego a 1×.
- Parametriza la trama: paridad, 7 vs 8 bits, 1 vs 2 stops.
