# Sample: 4-bit ripple-carry adder

Adding two binary numbers is the "hello world" of digital arithmetic. This sample builds a 4-bit adder with carry-in and carry-out — the exact cell you'd tile inside a bigger ALU or inside the address-increment logic of a CPU.

## Interface

```
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## The body

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

What's going on:

- `unsigned(a)` and `unsigned(b)` turn the vectors into numeric types for the `+` operator.
- `resize(…, 5)` pads to 5 bits so that the sum can overflow into the top bit.
- `("0000" & cin)` builds a 5-bit mask with just `cin` in the LSB.
- After the add, `t(3 downto 0)` is the sum and `t(4)` is the carry-out.

## Why 5 bits?

Adding two 4-bit numbers with a carry-in can produce a result as large as `1111 + 1111 + 1 = 11111` — five bits. Do the math in a 4-bit container and you'd lose the top bit silently. Resize is the safe idiom.

## What the sample does

The sim config drives `a` and `b` through four patterns (`3+5 = 8`, then a carry-generating `10 + 6 = 16`, then `10 + 15 = 25`…). The waveform shows `sum` and `cout` changing combinationally — no clock is needed because this design has no registers.

## Your turn

- **Make it parametric.** Add a `generic (N : integer := 4)` to the entity and use `N` wherever the 4 appears. Now you have an `adder_N` you can instantiate at any width.
- **Build a subtractor.** Flip `b` and add one more `+1` — that's two's complement subtraction. No extra hardware besides one NOT per bit.
- **Wire two of them up.** Put one for the low nibble and one for the high, and you've got an 8-bit adder with ripple-carry between them.
