## UART start-bit timing fix

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

This minimal UART receive FSM has four states: `IDLE`, `START`,
`DATA`, `STOP` (encoded `00`, `01`, `10`, `11` on `state`). With
the current code the FSM leaves `START` halfway through the bit
period and the rest of the frame samples in the wrong place.

### Specification

* 50 MHz `clk`, `BIT_PERIOD = 10` cycles per bit.
* From `IDLE`: `rx = '0'` moves the FSM to `START` immediately,
  **at the beginning of the bit period**.
* `START` and `DATA` each last exactly `BIT_PERIOD` cycles.
* `STOP` lasts one cycle then returns to `IDLE`.

### Hint

If the bit-period constant is half its real value, the FSM samples
the centre of every bit instead of the start. Re-derive the
constant from clock frequency and baud rate.
