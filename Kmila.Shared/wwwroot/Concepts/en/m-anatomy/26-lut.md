# Look-up table (LUT)

A **look-up table (LUT)** is how an FPGA becomes any gate: a tiny memory holding a truth table. The inputs form an **address** into it; the output is the stored bit at that address.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/26-lut.html" title="Interactive figure" loading="lazy"></iframe></div>

The inputs address the 4 config cells (the loaded truth table); the output is the selected bit. Watch the config **reprogram** — the same LUT becomes AND, then OR, then XOR. That is reconfigurable logic.

In VHDL:

```vhdl
y <= a and b;   -- the synthesizer loads this
                -- truth table into ONE LUT
```

Your whole design is mapped into LUTs: the synthesizer turns every boolean expression into truth tables and loads them at power-up from the configuration bitstream. An N-input LUT can be **any** function of N inputs.
