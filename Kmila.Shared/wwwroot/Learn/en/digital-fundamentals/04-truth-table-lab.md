# Lab: truth-table explorer

This lab is the first time you'll drive a real VHDL design. The entity has two inputs (`a`, `b`) and six outputs — one per elementary 2-input gate.

## What happens when you click *Open in the editor*

1. Kmila creates a throw-away project called something like "Lab: truth-table explorer".
2. It seeds it with `gates.vhd` containing the six concurrent assignments.
3. It loads a **simulation configuration** that stimulates `a` and `b` across every combination.
4. The editor workbench opens, with the config already selected in the pill at the top.

From there:

- Click **Build** to parse and elaborate the design.
- Click **Run** to simulate.
- Switch to the **Waveforms** tab in the dock at the bottom.

You'll see seven rows of square waves: your two inputs at the top, and each output reacting in real time.

## What to look for

Use the scroll bar or the keyboard to line up the input edges with the output transitions. Try to verify each gate's behaviour without looking back at the truth tables:

- `o_and`: stays low until both `a` and `b` are 1 — then pops high.
- `o_nand`: the opposite. It's usually high and goes low only when both inputs are 1.
- `o_xor`: high exactly when the two inputs disagree.

If one of them doesn't match what you'd expect, open the **Output** tab — build warnings land there.

## Try it yourself

Once the sample works, experiment:

- Add a third input `c` to the entity and compute `a xor b xor c` (three-input parity).
- Rename an output and rebuild — the **Ports** tab will update to match.
- Add another line to the config: flip `a` and `b` simultaneously and see which outputs change.

The more you move these levers before writing your own design, the less VHDL will feel like guessing.
