## Inverter — find and fix the bug

The author was asked to write a NOT gate (inverter): `y` should
be the **logical complement** of `a`. Right now it just copies
`a` straight to `y` — it acts as a buffer.

Find the bug and turn it into a real inverter.

### Specification

| `a` | `y` |
|:---:|:---:|
|  0  |  1  |
|  1  |  0  |

### Hint

A single VHDL operator does the whole job.
