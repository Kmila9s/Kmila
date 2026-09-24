## 4-bit up/down counter

Implement a 4-bit counter with a direction control. On a rising clock edge:

- if `dir = '1'`, count up: `count <= count + 1`
- if `dir = '0'`, count down: `count <= count - 1`

An asynchronous active-high `reset` clears the counter to `0000`.

### Hint

Use `IEEE.NUMERIC_STD.ALL` and an internal `unsigned(3 downto 0)` signal. Counting wraps modulo 16.
