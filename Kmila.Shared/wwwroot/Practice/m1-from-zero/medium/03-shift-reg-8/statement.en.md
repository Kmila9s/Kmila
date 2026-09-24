## 8-bit left shift register

Build an 8-bit shift register. On every rising clock edge, shift the contents left by one position and load the serial input `din` into bit 0. An asynchronous active-high `reset` clears the register.

### Spec

- Internal signal: `r : std_logic_vector(7 downto 0)`.
- Update rule: `r <= r(6 downto 0) & din;`.
- Output: `q <= r;`.

### Hint

Use a `process(clk, reset)` with `if reset = '1' then ... elsif rising_edge(clk) then ...`.
