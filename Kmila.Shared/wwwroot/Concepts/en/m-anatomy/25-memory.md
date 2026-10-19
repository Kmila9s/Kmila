# ROM memory

**Memory** stores words you can read back by **address**. A ROM holds fixed contents; each address selects one stored word, which appears at the output.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/25-memory.html" title="Interactive figure" loading="lazy"></iframe></div>

The address plungers pick a cell; its stored word lights the output lamps, and the table shows every address → data.

In VHDL:

```vhdl
type rom_t is array(0 to 3) of unsigned(2 downto 0);
constant ROM : rom_t := ("101","011","110","001");
...
data <= ROM(to_integer(addr));  -- addressed read
```

Reading is pure addressing — a ROM is literally an N-to-1 MUX per output bit. Writable RAM adds a write path; block RAM (BRAM) is dedicated memory built into the FPGA.
