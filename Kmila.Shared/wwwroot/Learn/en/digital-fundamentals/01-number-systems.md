# Number systems: bits, hex, two's complement

Digital circuits don't know about "seven" — they know about rows of switches that are either on or off. To design with them, you need to be comfortable moving between the representation humans like (decimal) and the representation silicon understands (binary), with hexadecimal as the useful shorthand in the middle.

## Binary (base 2)

A **bit** is a single on/off value, written `0` or `1`. A **byte** is eight bits.

Stacks of bits encode integers positionally, just like decimal — each place is a power of two:

```
  1 0 1 1 0
  │ │ │ │ └── 2⁰ = 1
  │ │ │ └──── 2¹ = 2
  │ │ └────── 2² = 0
  │ └──────── 2³ = 8
  └────────── 2⁴ = 16

  16 + 0 + 0 + 2 + 0 = 18
```

So `10110₂ = 18₁₀`.

## Hexadecimal (base 16)

Writing 32 bits in binary gives you a 32-character soup nobody can proofread:

```
00000000 00000000 11001010 11111110
```

Hex groups bits four at a time. Each hex digit covers exactly one nibble (4 bits), so 32 bits becomes a tidy 8-character string:

```
0x0000CAFE
```

The mapping is fixed:

| bin  | hex | bin  | hex |
|------|-----|------|-----|
| 0000 | 0   | 1000 | 8   |
| 0001 | 1   | 1001 | 9   |
| 0010 | 2   | 1010 | A   |
| 0011 | 3   | 1011 | B   |
| 0100 | 4   | 1100 | C   |
| 0101 | 5   | 1101 | D   |
| 0110 | 6   | 1110 | E   |
| 0111 | 7   | 1111 | F   |

In VHDL you'll write hex literals for vectors whose length is a multiple of four:

```vhdl
signal addr : std_logic_vector(7 downto 0) := x"CA";  -- 8 bits: 11001010
```

## Two's complement (signed integers)

Binary as shown above only encodes **non-negative** numbers. To fit negative numbers inside the same bit-pattern, digital designers use **two's complement**:

1. Write the positive number in binary.
2. Flip every bit.
3. Add 1.

To represent `-5` in 8 bits: `+5 = 00000101`, flip → `11111010`, add 1 → `11111011`.

Why this scheme? Because addition works identically whether the numbers are signed or not — you don't need two adders. `-5 + 7` with 8-bit two's complement:

```
  11111011   (-5)
+ 00000111   ( 7)
-----------
 100000010
```

The ninth bit overflows off the top, and the bottom 8 are `00000010 = 2`. Which is the right answer.

## What to remember

- Bits are the atoms; bytes are groups of 8.
- Binary and hex are the **same number** in different skins. Hex is easier to read.
- Two's complement encodes negatives by flipping-and-adding-one — and it's the reason `A + B` in your ALU works regardless of sign.
