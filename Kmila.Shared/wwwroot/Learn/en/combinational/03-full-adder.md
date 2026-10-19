# Sample: 4-bit ripple-carry adder

Adding two binary numbers is the "hello world" of digital arithmetic. This sample builds a 4-bit adder with carry-in and carry-out — the exact cell you'd tile inside a bigger ALU or inside the address-increment logic of a CPU. It's the first circuit in the combinational unit ("circuito sumador/restador") for a reason: once you can add, subtraction, comparison, and counting all follow.

## Start from one bit: the full adder

Before the 4-bit version, know the single-bit **full adder** it's made of. For bit `i`, with carry-in `cin`:

```
sum  = a xor b xor cin
cout = (a and b) or (cin and (a xor b))
```

The sum is the XOR of all three inputs (odd parity); the carry is "two or more of the three inputs are 1." Chain four of these — each one's `cout` feeding the next one's `cin` — and you have a **ripple-carry** adder. The name is literal: the carry has to ripple from the LSB all the way to the MSB before the top sum bit is final, which is why this structure is simple but not the fastest.

## Interface

```vhdl
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## The body

Rather than wiring four full adders by hand, let `numeric_std` do the arithmetic and just capture the carry:

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

What's going on:

- `unsigned(a)` and `unsigned(b)` reinterpret the vectors as numeric types for the `+` operator.
- `resize(…, 5)` zero-extends to 5 bits so the sum has room to overflow into the top bit.
- `("0000" & cin)` builds a 5-bit value with just `cin` in the LSB, so it adds in cleanly.
- After the add, `t(3 downto 0)` is the sum and `t(4)` is the carry-out.

> The two styles — four explicit full adders vs. one `+` on resized `unsigned` — synthesize to the *same* ripple-carry hardware (a modern tool may even promote it to a faster carry structure). The `+` version is what you'll actually write; the gate-level version is what you should be able to *explain*.

## Why 5 bits?

Adding two 4-bit numbers with a carry-in can produce a result as large as `1111 + 1111 + 1 = 11111` — five bits. Do the math in a 4-bit container and you'd lose the top bit silently. Resizing to one extra bit is the safe idiom, and bit 4 *is* your carry-out.

## Making a subtractor

The course pairs the adder with a subtractor, and in two's complement they're almost the same circuit. To compute `a − b`, add the bitwise complement of `b` and force the carry-in to 1:

```vhdl
t <= resize(unsigned(a), 5) + resize(unsigned(not b), 5) + "00001";
```

That's `a + (¬b) + 1 = a − b`. A real adder/subtractor adds one XOR per bit (controlled by an `add_sub` line) and feeds that same line into `cin` — one bit of control flips the whole unit between the two operations.

## What the sample does

The sim config drives `a` and `b` through several patterns (`3+5 = 8`, then a carry-generating `10 + 6 = 16`, then `10 + 15 = 25`…). The waveform shows `sum` and `cout` changing **combinationally** — no clock is needed because this design has no registers.

## Your turn

- **Make it parametric.** Add a `generic (N : integer := 4)` to the entity and use `N` wherever the `4` appears. Now you have an `adder_N` you can instantiate at any width.
- **Build the adder/subtractor** above with an `add_sub` control input.
- **Wire two of them up.** One for the low nibble, one for the high, with the first `cout` feeding the second `cin`, and you've got an 8-bit ripple-carry adder — and a feel for why the carry delay grows with width.

## What to remember

- A full adder is `sum = a xor b xor cin`, `cout = majority(a, b, cin)`; chain them to add wide numbers.
- Always give arithmetic one extra bit (`resize`) so the carry isn't lost.
- Subtraction is addition of the two's complement: `a − b = a + not b + 1`.
- Ripple-carry is simple but its delay grows with the number of bits.
