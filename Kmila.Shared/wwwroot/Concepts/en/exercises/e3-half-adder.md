**Your goal:** wire two output assignments using gate blocks and the input signals `a` and `b`.

A **half-adder** computes the sum of two single-bit numbers:

| `a` | `b` | `sum` | `cout` |
|---|---|---|---|
| 0 | 0 | 0 | 0 |
| 0 | 1 | 1 | 0 |
| 1 | 0 | 1 | 0 |
| 1 | 1 | 0 | 1 |

The math says: `sum = a XOR b`, `cout = a AND b`. Drag the blocks to match.

**Hint — try this exact tree:**

- An output-assign block with target **sum**, fed by an **XOR** block with `a` on the left and `b` on the right.
- Snap a second output-assign block underneath with target **cout**, fed by an **AND** block with `a` on the left and `b` on the right.

> Both assignments live outside a process, so they fire concurrently — the player will highlight both lines in the same delta cycle.
