# Sample: inferred synchronous ROM

Every FPGA has **block RAM** (BRAM) — dedicated memory blocks that the synthesizer uses when it recognises a ROM or RAM pattern in your code. You don't instantiate vendor primitives; you write plain VHDL in a shape the tool knows how to map. "Inference" is the name for that recognition: the tool reads your array-and-process and *infers* that you meant a memory.

This sample is the portable, textbook ROM pattern. Every serious synthesizer (Xilinx/AMD Vivado, Intel/Altera Quartus, Lattice Radiant, open-source Yosys) will recognise it and allocate BRAM automatically.

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

## Combinational vs. synchronous, and read latency

The clocked read is the whole game. Write the read as a plain concurrent assignment —

```vhdl
data <= ROM(to_integer(unsigned(addr)));   -- combinational: distributed ROM (LUTs)
```

— and the tool builds the ROM out of LUTs ("distributed" memory). That's zero read latency but costs general logic, and it doesn't scale: a few kilobytes of LUT-ROM will swamp a small device. Register the read (the `process` above) and the tool can drop the whole table into a block RAM, which is far denser.

The price of the registered read is **one cycle of latency**: you present `addr` on one edge, and `data` appears on the *next* edge. That's not a bug — it's the defining feature of a synchronous memory, and the BRAM's internal output register is what makes the design meet timing at high clock rates. Many FPGAs offer a second, optional output register for even better timing at the cost of a second cycle of latency.

## Address width must match the depth

`to_integer(unsigned(addr))` must never index past the array. For a 16-entry ROM, `addr` should be exactly 4 bits; a wider `addr` can produce an out-of-range index that fails in simulation and behaves unpredictably after synthesis. Keep `2**addr'length = DEPTH`, or clamp/guard the index. Tie the depth, the width, and the address size together and the whole thing stays consistent.

## Why these contents?

The example ROM holds `n²` for `n = 0..15`. Useful for a lookup-table squaring unit — faster than a full multiplier when you only need small operands. You could also store:

- A sine-wave table (for DDS signal generators)
- CRC polynomials
- Protocol lookup tables
- Character fonts

A ROM is really just a truth table frozen into silicon: any combinational function of the address bits can be precomputed and stored, trading logic for memory.

## What the sample does

The sim drives `addr` through 5 specific values: 0, 1, 4, 8, 15. After the clock edge, `data` shows the corresponding ROM entry, one cycle later (that's the read latency of a synchronous ROM — you ask for the address, you get the data on the **next** clock).

## Your turn

- **Make it parameterised.** Replace the fixed 16-entry size with `generic (DEPTH : integer := 16; WIDTH : integer := 8)`, and size `addr` as `std_logic_vector(log2(DEPTH)-1 downto 0)`. The rest of the code should keep working.
- **Read contents from a file.** Real projects load ROM contents via an initialisation function or a separate package of constants. Try making the VHDL read a text file of hex values (`std.textio` + `hread`) in an impure function that returns the array — most tools run it at elaboration and bake the result into BRAM.
- **Add a second read port.** For things like instruction/data memory in a single-cycle CPU. You'll see this pattern in the CPU project later in the curriculum.

## Ties to the course

This is Unit IV, *Memorias*: the ROM family (ROM, PROM, EPROM, EEPROM, Flash) and, specifically, *manejo de arreglos y diseño de memorias con lenguaje de descripción de hardware*. The `type … array … of` constant is exactly the "array handling" the unit asks for; the file-initialisation exercise reaches forward into Unit V's work with files, functions, and packages.

## What to remember

- Inference means writing memory in a shape the tool recognises — no vendor primitives needed.
- A **constant** array read inside a clocked process is the portable synchronous-ROM pattern; it maps to block RAM.
- Clocked read = one cycle of latency and dense BRAM; combinational read = zero latency but LUT-hungry and non-scalable.
- Match the address width to the depth so the index never runs off the end of the array.
