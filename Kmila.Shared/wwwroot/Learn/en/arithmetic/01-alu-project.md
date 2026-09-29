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
