# Sample: inferred synchronous ROM

Every FPGA has **block RAM** (BRAM) — dedicated memory blocks that the synthesizer uses when it recognises a ROM or RAM pattern in your code. You don't instantiate vendor primitives; you write plain VHDL in a shape the tool knows how to map.

This sample is the portable, textbook ROM pattern. Every serious synthesizer (Xilinx Vivado, Intel Quartus, Lattice Radiant, Yosys) will recognise it and allocate BRAM automatically.

## The pattern

```vhdl
type rom_t is array(0 to 15) of std_logic_vector(7 downto 0);
constant ROM : rom_t := (
  x"00", x"01", x"04", x"09", x"10", x"19", x"24", x"31",
  x"40", x"51", x"64", x"79", x"90", x"A9", x"C4", x"E1"
);

process(clk)
begin
  if rising_edge(clk) then
    data <= ROM(to_integer(unsigned(addr)));
  end if;
end process;
```

Three things make this a BRAM-friendly ROM:

1. **Storage is a constant** (`constant ROM : rom_t := (...)`). The data is fixed at synthesis time.
2. **The read is clocked.** A combinational ROM read can only be mapped to LUTs — that's fine for a tiny 16-entry ROM, but anything bigger needs the clocked pattern so the synthesizer allocates BRAM.
3. **`addr` is used purely as an index.** No expensive combinational logic between `addr` and the memory lookup.

## Why these contents?

The example ROM holds `n²` for `n = 0..15`. Useful for a lookup-table squaring unit — faster than a full multiplier when you only need small operands. You could also store:

- A sine-wave table (for DDS signal generators)
- CRC polynomials
- Protocol lookup tables
- Character fonts

## What the sample does

The sim drives `addr` through 5 specific values: 0, 1, 4, 8, 15. After the clock edge, `data` shows the corresponding ROM entry, one cycle later (that's the read latency of a synchronous ROM — you ask for the address, you get the data on the **next** clock).

## Your turn

- **Make it parameterised.** Replace the fixed 16-entry size with `generic (DEPTH : integer := 16; WIDTH : integer := 8)`. The rest of the code should keep working.
- **Read contents from a file.** Real projects load ROM contents via `init_from_file()` or a separate package of constants. Try making the VHDL read a text file of hex values (`textio` + `hread`) at elaboration time.
- **Add a second read port.** For things like instruction/data memory in a single-cycle CPU. You'll see this pattern in the CPU project later in the curriculum.
