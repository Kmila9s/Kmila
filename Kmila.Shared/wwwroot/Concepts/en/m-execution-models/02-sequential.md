# Sequential: one step at a time

Inside a `process` body, VHDL behaves a lot like the languages you already
know: statements execute **top to bottom, in source order**. Move a line
around and you change the result.

But there's a twist that catches everyone: **variables** and **signals**
behave differently.

| Kind | Syntax | When does the new value appear? |
|---|---|---|
| **Variable** | `tmp := a + 1` | **Immediately.** The next statement reads the new `tmp`. |
| **Signal** | `result <= tmp + b` | **At the end of the delta cycle.** Until then, reads still see the old value. |

The player walks through a tiny process that uses both. Notice how `tmp`
updates the moment we execute line 5, but `result` doesn't change until
*after* the process exits.

## Why this matters

Imagine you wrote:

```vhdl
result <= 1;
result <= 2;
```

In C, after these two lines `result` would clearly be `2`. In VHDL?
**Last assignment wins** — but only when the delta closes. While the
process runs, `result` is still its old value, and the simulator silently
discards the `<= 1` transaction because `<= 2` replaces it before the
delta ends.

> **Try this:** step to line 7 and look at the signal table. `tmp` is
> `8`, but `result` is still `U`. Step once more and watch `result`
> jump to `13` *after* the process body has finished.
