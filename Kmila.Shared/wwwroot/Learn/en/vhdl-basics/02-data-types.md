# Data types: `std_logic`, vectors, `unsigned`, `integer`

VHDL is a typed language. When you see a port declared `in std_logic`, the compiler won't let you wire it to something of a different type. Choosing the right type for each signal is half of writing clean VHDL.

## `std_logic` — the universal wire

A single bit of the **9-valued** IEEE logic. Yes, nine:

| Value | Meaning |
|-------|---------|
| `'0'` | strong drive low |
| `'1'` | strong drive high |
| `'Z'` | high impedance (tristate off) |
| `'L'` | weak low (pull-down) |
| `'H'` | weak high (pull-up) |
| `'X'` | unknown / multiple drivers disagreeing |
| `'U'` | uninitialised — your signal never got assigned |
| `'W'` | weak unknown |
| `'-'` | don't care (used in case statements) |

In synthesis only `'0'`, `'1'`, and `'Z'` make it to hardware. The others are simulation diagnostics — if you see `'U'` in a waveform, you have a signal that's read before it's written.

## `std_logic_vector` — a bus

A fixed-width array of `std_logic`:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, indexes 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, indexes 0..15
```

`downto` is by far the more common direction — it matches how we write numbers (MSB on the left).

Literals:

```vhdl
data <= "10101010";     -- binary literal, one char per bit
addr <= x"CAFE";        -- hex literal (must be multiple of 4 bits)
data <= (others => '0');-- shorthand for all-zeros
data <= (0 => '1', others => '0'); -- only the LSB high
```

## `unsigned` / `signed` — vectors you can do math on

`std_logic_vector` is "just a bag of bits". It has no numeric meaning. To add or subtract, you need `unsigned` or `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- works
```

Moving between them:

```vhdl
-- vector → unsigned for math
u <= unsigned(slv);

-- unsigned → vector for the outside world
slv <= std_logic_vector(u);

-- unsigned → integer for indexing or constants
idx <= to_integer(u);

-- integer → unsigned (specify width)
u <= to_unsigned(42, 8);
```

Rule of thumb: use `std_logic_vector` in **ports** (the widest audience), and convert to `unsigned` / `signed` inside architectures where you actually do arithmetic.

## `integer` — for indexing and counting

Use `integer` (or a constrained subtype) for loop counters, constants, and anything that won't cross a port boundary:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Unbounded `integer` is usually 32-bit in simulation. For hardware, you want a constrained range:

```vhdl
signal counter : integer range 0 to 255;
```

The synthesizer then gives you exactly 8 bits.

## Enumerations — for FSM states

Declare a readable type for state machines:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

You then write `state <= S_ACTIVE` instead of `state <= "010"`. The synthesizer picks the binary encoding for you (and you can hint with an attribute if you care).

## What to remember

- Single wire → `std_logic`. Bus → `std_logic_vector`.
- Need to do math? Use `unsigned` (or `signed`) and `IEEE.NUMERIC_STD`.
- Declaring **ports** in `std_logic_vector` makes your entity maximally portable.
- For FSM states, use enumerations — your code reads like a spec.
