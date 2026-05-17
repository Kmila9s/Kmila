**Your goal:** build a 4-bit counter that increments by one on every rising edge of the clock.

A **counter** is a tiny stateful machine — it remembers its current value between clock ticks. That kind of memory only works inside a clocked process.

**Hint — try this layout:**

1. Drag the **`on rising_edge(clk)`** wrapper block onto the canvas.
2. Drop the **`count <= count + 1`** block inside its body slot.

That's it. The player will show you the VHDL code that this composition represents, then walk through what happens on the first clock tick (count goes from `0000` to `0001`), the falling edge (no change), and the second rising edge (`0001` → `0010`).

> Sequential execution inside the process body — but the process itself is **woken** by a concurrent event (the clock signal changing). Both models cohabiting again.
