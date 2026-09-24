## UART — fix the start-bit timing

This minimal UART receive FSM has four states: `IDLE`, `START`,
`DATA`, `STOP` (encoded as `00`, `01`, `10`, `11` on `state`).
With the current code, the FSM advances out of `START` halfway
through the bit period and the rest of the frame samples in the
wrong place.

### Specification

* 50 MHz `clk`, `BIT_PERIOD = 10` clock cycles per bit.
* While in `IDLE`: a falling edge on `rx` (`rx = '0'`) moves the FSM
  to `START` immediately, **at the beginning of the bit period**.
* `START` and `DATA` each last exactly `BIT_PERIOD` cycles before
  advancing.
* `STOP` lasts one cycle then returns to `IDLE`.

### Hint

If the bit period constant is half its real value, the FSM samples
the centre of every bit instead of the start. Re-derive the
constant from `clk` frequency and baud rate.
