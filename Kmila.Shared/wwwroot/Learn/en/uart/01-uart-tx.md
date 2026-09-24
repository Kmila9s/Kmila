# Project: UART transmitter

A UART (Universal Asynchronous Receiver / Transmitter) is the first real protocol most embedded designers build. It's point-to-point, full duplex, and needs only three wires (TX, RX, GND). Every FTDI USB-serial adapter, every Arduino `Serial.print`, every legacy terminal is a UART underneath.

This project builds the **transmit** side. The receive side is an excellent next project once you finish this.

## Frame format (no parity, 8-bit data, 1 stop)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    line is normally high ('1'); start bit is a falling edge.
```

Each bit is held on `tx` for exactly `clk_freq / baud` clock cycles. For a 50 MHz clock and 115 200 baud, that's ≈ 434 cycles per bit.

## Your job

1. Build a clock-counter that produces a one-cycle `baud_tick` every ~434 cycles.
2. On `send = '1'`, latch `data` into a 10-bit shift register: `{ stop_bit, data[7..0], start_bit } = 1 & data & 0`. Drive `busy <= '1'`.
3. On each `baud_tick`, shift the register right (so the start bit goes out first, then LSB through MSB of data, then the stop bit).
4. After the 10th tick, drop `busy` and idle `tx` high.

## Scaffold

The entity and idle state are provided; `tx` defaults high, `busy` defaults low. Everything between those defaults is your responsibility.

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## Suggested structure

Two counters + one shift register:

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- When `send = '1'` and `sending = '0'`:
  - load `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- While `sending = '1'`:
  - `clk_count <= clk_count + 1;`
  - if `clk_count = BAUD_PERIOD - 1`:
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — shift right, pad top with idle
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - if `bit_count = 10`:
    - `sending <= '0';`

Make `BAUD_PERIOD` a generic so you can shrink it for simulation (e.g., 5 cycles/bit) and still have the same logic work at full speed on hardware.

## What to test

Drive `send = '1'` with `data = "01001000"` (ASCII `'H'`). After the frame completes, the `tx` waveform should show exactly 10 bit-periods of activity:

1. Start (low)
2. Bit 0 of data (which is the LSB: `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Stop (high)

Then back to idle high. Zoom into the Waveforms tab and verify the pattern bit by bit.

## Going further

- Add a **FIFO** so you can queue multiple bytes; `send` becomes "push".
- Build the **RX** side: sample each incoming bit at 1.5 × bit-period, then at 1 × from there on.
- Parameterise the frame: parity, 7-bit vs 8-bit data, 1 vs 2 stop bits.
