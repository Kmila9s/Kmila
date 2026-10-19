# Data types: `std_logic`, vectors, `unsigned`, `integer`

VHDL is a **strongly typed** language. When you see a port declared `in std_logic`, the compiler won't let you wire it to something of a different type without an explicit conversion. Choosing the right type for each signal is half of writing clean VHDL.

## `std_logic` — the universal wire

A single bit of the **9-valued** IEEE 1164 logic. Yes, nine:

| Value | Meaning |
|-------|---------|
| `'0'` | strong drive low |
| `'1'` | strong drive high |
| `'Z'` | high impedance (tristate off) |
| `'L'` | weak low (pull-down) |
| `'H'` | weak high (pull-up) |
| `'X'` | strong unknown — e.g. two drivers fighting with different values |
| `'U'` | uninitialised — your signal never got assigned |
| `'W'` | weak unknown |
| `'-'` | don't care (used as a `std_match` pattern / synthesis hint) |

Why nine instead of two? Because a real wire isn't just "0 or 1". It can be floating (`'Z'`), pulled weakly (`'L'`/`'H'`), or in genuine conflict (`'X'`). This is what lets `std_logic` be a **resolved** type: when several drivers share one wire, a resolution function decides the result. A strong value beats a weak one; two strong values that disagree produce `'X'`; and an undriven wire sits at `'Z'`.

In synthesis only `'0'`, `'1'`, and `'Z'` make it to hardware. The others are simulation diagnostics — if you see `'U'` in a waveform, you have a signal that's read before it's written; a stripe of `'X'` usually means a bus with two active drivers.

> There is also a non-resolved cousin, `std_ulogic` (the `u` is "unresolved"). `std_logic` is literally `std_ulogic` plus a resolution function. Use `std_logic` by default — the resolution is what you want for buses.

## `std_logic_vector` — a bus

A fixed-width array of `std_logic`:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, indexes 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, indexes 0..15
```

`downto` is by far the more common direction — it matches how we write numbers (MSB on the left, so `data(7)` is the most significant bit). Pick one direction per project and stick to it; mixing `to` and `downto` is a classic source of silently reversed buses.

Literals:

```vhdl
data <= "10101010";      -- binary literal, one char per bit
addr <= x"CAFE";         -- hex literal (width must be a multiple of 4 bits)
data <= (others => '0'); -- aggregate: all-zeros, any width
data <= (0 => '1', others => '0'); -- only bit 0 (the LSB) high
```

The `(others => '0')` form is worth loving: it adapts automatically if you later change the width, so it never goes stale.

## `unsigned` / `signed` — vectors you can do math on

`std_logic_vector` is "just a bag of bits". It has no numeric meaning, so `slv1 + slv2` is a compile error with `numeric_std`. To add or subtract, you need `unsigned` or `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- works: same-width unsigned addition
```

The two types differ only in how the top bit is read: `unsigned` treats all bits as magnitude; `signed` treats the MSB as a two's-complement sign.

Moving between them — these are the four conversions you'll use constantly:

```vhdl
-- reinterpret the same bits as unsigned (for math)
u   <= unsigned(slv);

-- reinterpret an unsigned as plain bits (for the outside world / a port)
slv <= std_logic_vector(u);

-- unsigned -> integer, for indexing or constants
idx <= to_integer(u);

-- integer -> unsigned; you MUST give the width
u   <= to_unsigned(42, 8);
```

`unsigned(slv)` and `std_logic_vector(u)` are **type casts** — they change the label on the bits, not the bits. `to_integer` and `to_unsigned` are **functions** that actually compute. A common beginner error is calling `to_unsigned` on a vector (it takes an integer) or casting an integer with `unsigned(…)` (casts only work between the array types).

One more that saves overflow bugs — `resize`, which pads or truncates to a new width. It **zero-extends** `unsigned` and **sign-extends** `signed`, so it preserves the value before arithmetic:

```vhdl
wide <= resize(a, 9) + resize(b, 9);  -- 9-bit result keeps the carry
```

Rule of thumb: use `std_logic_vector` in **ports** (the widest audience), and convert to `unsigned` / `signed` inside architectures where you actually do arithmetic.

## `integer` — for indexing and counting

Use `integer` (or a constrained subtype) for loop counters, constants, and anything that won't cross a port boundary:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Unbounded `integer` is at least 32-bit in simulation (range −2³¹+1 to 2³¹−1). For hardware you almost always want a constrained range so the synthesizer sizes the register:

```vhdl
signal counter : integer range 0 to 255;  -- synthesizes to exactly 8 bits
```

An unconstrained `integer` counter will cost you 32 flip-flops you didn't want.

## Enumerations — for FSM states

Declare a readable type for state machines:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

You then write `state <= S_ACTIVE` instead of `state <= "010"`. The synthesizer picks the binary (or one-hot) encoding for you, and you can hint it with an attribute if you care. Your code reads like a spec, and the waveform shows the state *names*, not raw bits.

## What to remember

- Single wire → `std_logic`. Bus → `std_logic_vector`. Both come from `STD_LOGIC_1164`.
- Need to do math? Use `unsigned` (or `signed`) and `IEEE.NUMERIC_STD`. `std_logic_vector` has no numeric meaning.
- `unsigned(x)` / `std_logic_vector(x)` are casts (relabel bits); `to_integer` / `to_unsigned` are functions (and `to_unsigned` needs a width).
- Declaring **ports** in `std_logic_vector` makes your entity maximally portable; convert inside.
- Constrain your `integer` ranges, and use enumerations for FSM states.
