## UART receiver FSM

A simplified UART receiver controller. The 4-state machine cycles:

| State | Code | Meaning              |
|:-----:|:----:|:---------------------|
| Idle  | `00` | Wait for `rx = '0'`  |
| Start | `01` | Capture start bit    |
| Data  | `10` | Sample data bit      |
| Stop  | `11` | Capture stop bit     |

### Spec

- From idle, advance to start only when `rx = '0'`.
- From start/data/stop, advance unconditionally on the next clock.
- Stop wraps back to idle.
- Reset → idle.

### Hint

A 2-bit `STD_LOGIC_VECTOR` plus a `case` statement is the simplest encoding.
