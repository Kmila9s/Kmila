## 4-bit adder

Implement a 4-bit ripple-carry-style adder with carry-in `cin`. Compute `sum = a + b + cin` and the carry-out `cout`.

### Spec

- Inputs: `a`, `b` are 4-bit `STD_LOGIC_VECTOR`; `cin` is a single bit.
- Outputs: `sum` is 4 bits; `cout` is one bit.

### Hint

Use `IEEE.NUMERIC_STD.ALL` and a 5-bit `unsigned` temporary:

```vhdl
signal s : unsigned(4 downto 0);
s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
sum  <= std_logic_vector(s(3 downto 0));
cout <= s(4);
```
