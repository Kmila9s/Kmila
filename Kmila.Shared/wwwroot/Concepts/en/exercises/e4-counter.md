**Your goal:** build a counter that adds **one** on every rising edge of the clock. Put the `count <= count + 1` block inside the `on rising_edge(clk)` wrapper and press **Run** — the player clocks it four times.

Try the experiments too: an increment **outside** the wrapper (no clock at all), or **two** increments inside it. The verdict explains what really happens in VHDL.
