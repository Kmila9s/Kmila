## 4-bit synchronous counter

Build a 4-bit counter that increments on every rising clock edge. An asynchronous active-high `reset` clears the count to `0000`.

### Spec

- Internal `count : unsigned(3 downto 0) := (others => '0')`.
- On `rising_edge(clk)`: `count <= count + 1;`.
- Output: `q <= std_logic_vector(count);`.

### Hint

Use `IEEE.NUMERIC_STD.ALL` so `+ 1` works on `unsigned`.
