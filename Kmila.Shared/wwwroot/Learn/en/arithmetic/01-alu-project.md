# Project: 8-bit ALU with flags

An **Arithmetic Logic Unit** is the beating heart of any CPU. Give it two operands and an opcode; it gives you back a result and a handful of flags that describe what happened (did it overflow? is the result zero? etc.).

## What you're building

An 8-bit ALU with the following operations:

| Opcode | Name    | Formula                   |
|--------|---------|---------------------------|
| `000`  | ADD     | `y = a + b`               |
| `001`  | SUB     | `y = a - b`               |
| `010`  | AND     | `y = a and b`             |
| `011`  | OR      | `y = a or  b`             |
| `100`  | XOR     | `y = a xor b`             |
| `101`  | SHL     | `y = a shifted left by 1` |
| `110`  | SHR     | `y = a shifted right by 1`|
| `111`  | PASS_A  | `y = a` (pass-through)    |

Plus two flag outputs:

- `zero` — high when `y` is all zeros
- `carry` — the carry-out from ADD/SUB; drive `'0'` for everything else

In the ESCOM *Arquitectura de Computadoras* course this is topic 3.1, the first functional block of the single-cycle RISC processor. Build it cleanly now and you'll drop it straight into the datapath later.

## The adder underneath

ADD and SUB look like one line of VHDL (`a + b`), but the hardware the synthesiser builds for that `+` is where the interesting engineering lives. The course asks you to compare two classic structures.

**Ripple-carry adder.** Chain eight full adders, feeding each stage's carry-out into the next stage's carry-in. It's tiny and obvious, but the carry has to *ripple* from bit 0 all the way to bit 7 before the top sum bit is valid — so the worst-case delay grows linearly with the word width. For an 8-bit adder that's eight full-adder delays stacked end to end.

**Carry-lookahead adder (CLA).** Instead of waiting for the ripple, compute each carry directly from the inputs. Define two signals per bit:

```
generate:  g(i) = a(i) and b(i)      -- this bit makes a carry on its own
propagate: p(i) = a(i) xor b(i)      -- this bit passes an incoming carry
```

Then every carry is `c(i+1) = g(i) or (p(i) and c(i))`, expanded so the carries are produced in parallel rather than one after another. The delay becomes roughly constant with width, at the cost of more gates. This is the "acarreo anticipado" (anticipated carry) the syllabus contrasts against the cascaded "acarreo en cascada" ripple design.

For this project you don't have to hand-build either one — writing `a + b` lets the FPGA tools pick an optimised carry chain (Artix-7 fabric even has dedicated fast-carry logic). The point is to *understand* what trade-off that `+` represents: area versus speed.

## Getting the flags right

Flags are where beginners quietly lose points, because **carry** and **overflow** are two different things:

- **Carry (C)** is the bit that falls off the top of an *unsigned* addition. It's the 9th bit of `a + b`.
- **Overflow (V)** signals that a *signed* (two's-complement) result no longer fits in 8 bits. The clean rule: `V = carry_into_MSB xor carry_out_of_MSB`. Equivalently, overflow happened if two same-sign operands produced an opposite-sign result.

A tidy VHDL trick for both at once is to add in a 9-bit space:

```vhdl
signal ext : unsigned(8 downto 0);
...
ext   <= ('0' & unsigned(a)) + ('0' & unsigned(b));  -- 9-bit add
result <= std_logic_vector(ext(7 downto 0));
cout   <= ext(8);                                     -- the carry flag
```

For SUB, remember subtraction is `a + (not b) + 1` in two's complement, so `15 - 15` internally generates a carry-out of `1` even though the result is zero — which is exactly why many ISAs treat the subtract carry as a *borrow* (C = not borrow). Keep your convention consistent with whatever the companion testbench expects.

## What the scaffold gives you

The starter declares the entity, the `result` and `cout` signals, and a `process(op, a, b)` shell that already handles ADD and SUB. Your job is to fill in the other six branches of the `case` statement.

## Hints

- For **AND / OR / XOR**, it's a one-liner each — just assign the bitwise result:

  ```vhdl
  when "010" => result <= a and b;
  ```

- For **SHL** and **SHR**, pad with `'0'`:

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- For **PASS_A**, just copy `a` into `result`.

Remember to drive `cout <= '0'` for logic ops — you already do it once at the top of the process, which means any branch that doesn't override it inherits that default. Handy, but it means you have to be explicit when a branch **does** want a non-zero carry.

## Acceptance criteria

With the stimuli in the companion config:

| time | op    | a          | b          | expected `y` | `carry` | `zero` |
|------|-------|------------|------------|--------------|---------|--------|
|  0   | `000` | `00001111` | `00000101` | `00010100`   | `0`     | `0`    |
| 20   | `001` | `00001111` | `00001111` | `00000000`   | `0`     | `1`    |
| 40   | `010` | `00001111` | `00001111` | `00001111`   | `0`     | `0`    |
| 60   | `101` | `10110000` | n/a        | `01100000`   | `0`     | `0`    |
| 80   | `111` | `10110000` | n/a        | `10110000`   | `0`     | `0`    |

## Going further

- Add a **signed** flag (negative result for two's-complement SUB).
- Add a **rotate** opcode — same as SHL but the top bit wraps back to bit 0.
- Add a **multiply** opcode producing a 16-bit result on two separate output ports. Most small FPGAs have dedicated DSP blocks for this — watch your utilisation when you resynthesise.

This ALU is a direct building block for the CPU project at the Expert level — keep this design clean — you'll wire it up there.

## What to remember

- An ALU is operands-in, result-and-flags-out. The opcode selects which operation the one block performs.
- The `+` you write hides a real choice: ripple-carry (small, slow, delay grows with width) versus carry-lookahead (bigger, fast, delay roughly constant) using generate/propagate terms.
- **Carry ≠ overflow.** Carry is the unsigned 9th bit; overflow is `carry_in(MSB) xor carry_out(MSB)` and only matters for signed values.
- Set a sensible default for every flag at the top of the process, then override only where a branch truly needs to — that's how you avoid latches and forgotten cases.
