# The seven logic gates

Every digital circuit, down to a modern GPU, is a combination of seven elementary cells. If you know how each one behaves and how to write it in VHDL, you've already started doing hardware design.

## The six two-input gates

| Gate  | Symbol in VHDL | Reads as | Output is `1` when… |
|-------|----------------|----------|---------------------|
| AND   | `a and b`      | "A and B"  | both inputs are 1 |
| OR    | `a or b`       | "A or B"   | at least one input is 1 |
| XOR   | `a xor b`      | "A xor B"  | inputs are different |
| NAND  | `a nand b`     | "not (A and B)" | at least one input is 0 |
| NOR   | `a nor b`      | "not (A or B)"  | both inputs are 0 |
| XNOR  | `a xnor b`     | "A = B"    | inputs are equal |

And the one-input NOT:

```vhdl
y <= not a;
```

## Why NAND and NOR are special

A classic result in digital logic: **NAND alone** (or NOR alone) is enough to build every other gate. Both are called "functionally complete" for exactly that reason. This is why CMOS process libraries ship lots of NAND2 and NOR2 cells — you can synthesise everything from them.

- NOT: `not a = a nand a`
- AND: `a and b = not (a nand b)`
- OR:  `a or b = (not a) nand (not b)`

## XOR is the mathy one

XOR is often drawn with a circle-plus (⊕). It's the gate that computes "parity" — its output is `1` when an odd number of inputs are `1`. That makes it the core of:

- Adders: `sum = a xor b xor carry_in`
- Parity checkers and error-detection codes
- Toggle flip-flops: `q_next = q xor toggle_input`

## Seeing all six in one file

If you open the **truth-table explorer** lab for this module, you'll find a single VHDL entity that computes all six 2-input gates at once, with the inputs `a` and `b` wired to the ports tab for stimulus. The waveform viewer then shows each output's reaction at every combination.

```vhdl
architecture rtl of gates is
begin
  o_and  <= a and b;
  o_or   <= a or  b;
  o_xor  <= a xor b;
  o_nand <= a nand b;
  o_nor  <= a nor  b;
  o_xnor <= a xnor b;
end architecture rtl;
```

Notice each of those six lines is **a wire**, not a statement that runs. Six gates, six pieces of hardware — they all evaluate simultaneously.

## What to remember

- Six two-input gates + NOT is the full set.
- NAND and NOR are "universal" — you can build anything from them.
- In VHDL, gates are one-liner concurrent assignments. They aren't "called"; they exist.
