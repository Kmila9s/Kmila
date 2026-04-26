# Boolean algebra and the laws that matter

Boolean algebra is the mathematics of true/false values and the operations you can perform on them. Every digital circuit — from a single gate to a million-transistor CPU — is ultimately a boolean expression.

The good news: you only need to remember a handful of identities. With those you can simplify almost any expression by hand.

## The three basic operations

| Op | Symbol (math) | Symbol (VHDL) | Reads as |
|----|---------------|---------------|----------|
| AND | · or juxtaposition | `and` | "A and B" |
| OR  | +                  | `or`  | "A or B"  |
| NOT | overline (`Ā`)     | `not` | "not A"   |

Truth tables for the binary operators:

```
 A  B  │  A·B   A+B
──────┼────────────
 0  0  │   0     0
 0  1  │   0     1
 1  0  │   0     1
 1  1  │   1     1
```

## The identities you'll actually use

**Commutativity** — order doesn't matter:

```
A · B = B · A
A + B = B + A
```

**Associativity** — grouping doesn't matter:

```
(A · B) · C = A · (B · C)
(A + B) + C = A + (B + C)
```

**Distributivity** — the one that looks weird in the OR case:

```
A · (B + C) = A·B + A·C
A + (B · C) = (A + B) · (A + C)   ← yes, really
```

**Identity and dominance**:

```
A + 0 = A      A · 1 = A
A + 1 = 1      A · 0 = 0
```

**Complement**:

```
A + Ā = 1      A · Ā = 0
```

**De Morgan's laws** — the two most useful rewrites in digital design:

```
not(A · B) = Ā + B̄
not(A + B) = Ā · B̄
```

De Morgan lets you turn a NAND into "OR of complements", or an NOR into "AND of complements", or cancel long NOT bars. Every FPGA toolchain uses them constantly during optimisation.

## A worked simplification

Start with:

```
Y = A·B + A·B̄·C + A·B̄·C̄
```

Factor `A` out:

```
Y = A · (B + B̄·C + B̄·C̄)
```

Factor `B̄` inside:

```
Y = A · (B + B̄ · (C + C̄))
```

`C + C̄ = 1` (complement), so:

```
Y = A · (B + B̄ · 1)
  = A · (B + B̄)
  = A · 1
  = A
```

Four terms with five AND-OR operations collapse to a wire. This is the kind of work the synthesizer does for you automatically — but understanding it makes you sharper at reading someone else's netlist.

## What to remember

- AND / OR / NOT are all you need. NAND and NOR are AND/OR with a NOT stuck on.
- De Morgan is the Swiss army knife — learn to recognise it in both directions.
- When a subexpression evaluates to `1` with complement, the whole product simplifies.
