# Project: UART transmitter

A UART (Universal Asynchronous Receiver / Transmitter) is the first real protocol most embedded designers build. It's point-to-point, full duplex, and needs only three wires (TX, RX, GND). Every FTDI USB-serial adapter, every Arduino `Serial.print`, every legacy terminal is a UART underneath.

This project builds the **transmit** side. The receive side is an excellent next project once you finish this.

## Asynchronous: there is no clock wire

The "A" in UART is the whole challenge. Unlike SPI or I2C, the two ends share **no clock line**. The receiver has to recover timing from the data itself. That works only if both ends agree in advance on the **baud rate** (bits per second) and the **frame format**. Get either wrong and you receive garbage — the classic symptom of a baud mismatch.

In the ESCOM course this sits under the RS-232 standard. Worth separating two things people conflate: the **UART** is the digital logic that serialises bytes (what you build here, at 0 V / 3.3 V logic levels), while **RS-232** is the electrical standard that shifts those levels to roughly ±12 V with *inverted* polarity for driving a cable. A MAX232-style line driver sits between your UART pin and a real DB-9 connector. Your VHDL only ever deals with the clean logic-level side.

## Frame format (no parity, 8-bit data, 1 stop)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    line is normally high ('1'); start bit is a falling edge.
```

This is "8N1": **8** data bits, **N**o parity, **1** stop bit — 10 bits on the wire per byte. The rules that matter:

- **Idle** is high. A broken or disconnected line reads as idle, not as a stream of zeros.
- **Start** bit is low. Its falling edge is the only timing reference the receiver gets.
- **Data** goes out **LSB first** — bit 0 before bit 7.
- **Stop** bit is high, which also guarantees a fresh falling edge is available for the next frame.

Each bit is held on `tx` for exactly `clk_freq / baud` clock cycles. For a 50 MHz clock and 115 200 baud, that's 50 000 000 / 115 200 ≈ 434 cycles per bit.

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

## How much timing error you can afford

Because there's no shared clock, the receiver samples near each bit's centre and drifts a little across the frame. Over 10 bit-periods the accumulated error must stay under half a bit, so each end's baud error has to stay under about ±2–3 %. Integer dividers rarely land exactly: at 50 MHz, 434 cycles/bit gives 115 207 baud — an error of 0.006 %, completely safe. An awkward clock can round badly, which is why many real UARTs add a fractional divider. The receive side earns margin by **oversampling** at 16× the baud rate: find the start edge, then sample at count 8 (the bit centre) and every 16 ticks after.

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

## Common pitfalls

- **Wrong bit order.** Sending MSB first is the single most common bug. The scaffold's `'1' & data & '0'` plus a right shift gets LSB-first for free — don't "fix" it.
- **Off-by-one on the bit count.** 8N1 is *10* ticks, not 8. Forgetting the start or stop bit truncates the frame.
- **Re-triggering mid-frame.** Honour `send` only when `sending = '0'`, or a second request will corrupt the one in flight. That's what `busy` is for.
- **Baud mismatch.** If the receiver shows the right number of bytes but scrambled values, suspect the divider before the logic.

## Going further

- Add a **FIFO** so you can queue multiple bytes; `send` becomes "push".
- Build the **RX** side: oversample 16×, find the start edge, sample at the bit centre.
- Parameterise the frame: parity, 7-bit vs 8-bit data, 1 vs 2 stop bits.

## What to remember

- UART is **asynchronous**: no clock wire, so both ends must agree on baud rate and frame format up front.
- 8N1 is **10 bits** on the wire — idle high, start low, 8 data bits **LSB first**, stop high.
- One bit lasts `clk_freq / baud` cycles; a one-cycle `baud_tick` paces the shift register.
- Keep per-end baud error under ~2 %; the RX side buys margin by **oversampling and sampling at the bit centre**.
- **UART** is the logic; **RS-232** is the ±12 V electrical layer a line driver adds on top.
