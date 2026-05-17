**Your goal:** imagine four cooks in a restaurant kitchen, each at their own station. They don't wait for each other — every task starts at the same instant. Drag four tasks into the workspace and press **Run**.

**Hint — try this set of tasks:**

1. Chop the vegetables
2. Marinate the meat
3. Fry the eggs
4. Boil the pasta

> The blocks look the same as in the sequential pizza exercise, but the player will tell a different story: all four lines highlight together. That's the **concurrent** execution model — same blocks, different semantics, because we're outside any sequential process.

This is what concurrent signal assignments in a VHDL `architecture` body look like: every line fires in the same delta cycle.
