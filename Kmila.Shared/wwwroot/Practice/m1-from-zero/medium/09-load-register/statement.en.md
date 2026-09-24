## Load register (4-bit)

Implement a 4-bit register with synchronous load. On a rising clock edge, if `load = '1'`, copy `d` into the register. Otherwise hold the previous value. An asynchronous active-high `reset` clears it to `0000`.

### Spec

- Internal: `signal r : std_logic_vector(3 downto 0) := (others => '0');`.
- Process: `if load = '1' then r <= d;`.
- Output: `q <= r;`.

### Hint

A common building block — the same shape as a counter, but the new value comes from `d` instead of `count + 1`.
