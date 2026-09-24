## 4-bit ALU — missing operation

A 4-operation ALU was specified, selected by `op`:

| `op` | operation |
|:----:|-----------|
| `00` | `a + b`   |
| `01` | `a - b`   |
| `10` | `a and b` |
| `11` | `a xor b` |

### What's wrong

For `op = "11"` the output is hardwired to `"0000"` instead of computing `a xor b`. Add (`00`), subtract (`01`) and AND (`10`) all behave correctly.

### Hint

Look at the `when others` branch of the `case` statement. The expected result for that branch is a bitwise `xor`, not a constant.
