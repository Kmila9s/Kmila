## 8 ビット ALU

⚠ 翻訳待ち — 説明は英語で表示されます。

## 8-bit ALU

Implement a combinational 8-bit ALU. The 2-bit `op` selects the operation:

| `op` | Operation | Output `y`     |
|:----:|:----------|:---------------|
| `00` | ADD       | `a + b`        |
| `01` | SUB       | `a - b`        |
| `10` | AND       | `a and b`      |
| `11` | OR        | `a or b`       |

### Spec

- Combinational only — no clock, no flip-flops.
- Use `IEEE.NUMERIC_STD` and treat operands as `unsigned`.
- Cast back to `STD_LOGIC_VECTOR` for the output.

### Hint

A `case op is ... when others =>` inside a process gives a clean and synthesisable selector.
