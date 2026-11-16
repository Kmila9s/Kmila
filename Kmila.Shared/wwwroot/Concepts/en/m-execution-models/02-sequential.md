# Sequential: one step at a time

Inside a `process` body, VHDL behaves a lot like the languages you already know: statements execute **top to bottom, in source order**. Move a line around and you change the result.

But there's a twist that catches everyone: **variables** and **signals** behave differently.

| Kind | Syntax | When does the new value appear? |
|---|---|---|
| **Variable** | `tmp := a + 1` | **Immediately.** The next statement reads the new `tmp`. |
| **Signal** | `result <= tmp + b` | **At the end of the delta cycle.** Until then, reads still see the old value. |

The player walks through a tiny process that uses both. Notice how `tmp` updates the moment we execute line 5, but `result` doesn't change until *after* the process exits.

<!-- exec-fig2:start -->
## The notepad and the mailbox

A **variable** is like a notepad in the worker's pocket: when line 4 or 5 runs, the new number is written on it right away, and the next line already reads it. A **signal** is like a letter: `result <= tmp + b` drops the letter (13) in the outbox, but `result` keeps showing U until the **mail truck** passes by — the end of the delta cycle, marked δ — and delivers it.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-mailbox.html" title="Desk with a notepad (variable) and an outbox, a mail truck and a mailbox (signal)" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

The figure plays the same steps as the player below. Click the dots under the code to jump to a step, or click the drawing to pause.
<!-- exec-fig2:end -->

## Why this matters

Imagine you wrote:

```vhdl
result <= 1;
result <= 2;
```

In C, after these two lines `result` would clearly be `2`. In VHDL? **Last assignment wins** — but only when the delta closes. While the process runs, `result` is still its old value, and the simulator silently discards the `<= 1` transaction because `<= 2` replaces it before the delta ends.

> **Try this:** step to line 7 and look at the signal table. `tmp` is `8`, but `result` is still `U`. Step once more and watch `result` jump to `13` *after* the process body has finished.
