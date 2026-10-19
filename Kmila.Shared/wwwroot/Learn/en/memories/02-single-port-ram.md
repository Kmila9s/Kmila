# Sample: single-port RAM with write-first read

Read-write memory is the other half of "inferring BRAM". The same `type…array…of` pattern works, but now the array is a **signal**, and you write on some clock edges and read on the others.

## The template

```vhdl
type mem_t is array(0 to 31) of std_logic_vector(7 downto 0);
signal mem : mem_t := (others => (others => '0'));
...
process(clk)
begin
  if rising_edge(clk) then
    if we = '1' then
      mem(to_integer(unsigned(addr))) <= din;
      dout <= din;                             -- write-first read
    else
      dout <= mem(to_integer(unsigned(addr)));
    end if;
  end if;
end process;
```

Why a **signal** and not a `variable` or constant? The array has to be shared state that persists across clock edges and gets *rewritten* at runtime — that's exactly what a signal is. A constant (as in the ROM sample) can't be written; a process variable wouldn't be visible outside the process. And because every access is inside `rising_edge(clk)`, both the write and the read are **synchronous** — which is what lets the tool map the whole thing to a block RAM.

## The three read policies

Every synchronous RAM has to decide what `dout` does **during a write** to the address being read. This "read-during-write" behaviour has three standard conventions, and the shape of your process picks which one you get:

- **Write-first** (also called read-through, shown above): on a write, `dout` shows the newly-written data. Cleanest semantics — a read and a write to the same address in the same cycle give you the fresh value.
- **Read-first:** on a write, `dout` shows the **old** contents of the cell. Some CPUs' register files use this.
- **No-change:** on a write, `dout` doesn't update at all — it holds its last value. Common in low-power designs, since the output register isn't toggled on writes.

The difference is purely the *order* of the two assignments in the process:

```vhdl
-- READ-FIRST: capture old data, then overwrite
if we = '1' then
  dout <= mem(to_integer(unsigned(addr)));   -- old value out
  mem(to_integer(unsigned(addr))) <= din;    -- then write
end if;

-- NO-CHANGE: only update dout when NOT writing
if we = '1' then
  mem(to_integer(unsigned(addr))) <= din;    -- dout left untouched
else
  dout <= mem(to_integer(unsigned(addr)));
end if;
```

All three map to block RAM on any modern FPGA, and each port of a true dual-port RAM can even use a different mode. But the synthesizer picks the mode from the *exact* shape of your process, so match the template to the behaviour you actually want. The write-first form is the most broadly portable — if in doubt, write it.

## What the sample does

For the first 30 µs the sim writes to three addresses (`0`, `1`, `2`) with three different byte patterns. Then `we` drops, and `addr` is reset back to 0 at 40 µs. The Waveforms tab shows `dout` tracking `din` during writes, then producing the stored value back out when you read — one cycle after you present the address, because the read is registered.

## Watch out for

- **Address width vs. depth.** Just like the ROM, `to_integer(unsigned(addr))` must stay inside `0 .. 31`. Size `addr` to `log2(DEPTH)` bits so it can't over-index.
- **Don't read combinationally.** A combinational read (`dout <= mem(...)` outside the clocked process) usually blocks BRAM inference and forces the array into flip-flops/LUTs — fine for a dozen bytes, disastrous for kilobytes.
- **Initialisers aren't guaranteed.** The `signal mem := (others => ...)` initial value works in simulation and on many FPGAs (where BRAM has a power-up state), but not on all targets and never on real external SRAM/DRAM, which power up undefined. Don't rely on it in production without checking your device.

## Your turn

- **Upgrade to a true dual-port RAM.** Two full sets of `addr`/`din`/`dout`/`we`, both clocked by `clk`. This is the pattern every vendor's FIFOs and dual-clock DPRAMs use underneath.
- **Add an output register.** Block RAM supports a second pipeline register after the memory array. Add `dout_reg <= dout;` in a second clocked block and watch what changes on the waveform (hint: one more cycle of latency, but much better timing closure).
- **Build a FIFO on top.** Wrap the RAM with a write pointer, a read pointer, and `full`/`empty` flags. That's the standard way a synchronous FIFO is built, and it's called for by name in this unit.

## Ties to the course

This is the core of Unit IV, *Memorias*: RAM / SRAM / DRAM and the design of RAM, ROM, and **FIFO** memories in HDL via array handling. Write-first/read-first/no-change is the HDL face of the read-during-write behaviour real SRAM cells exhibit; the FIFO exercise above is the exact *tipos especiales de memorias* the unit lists.

## What to remember

- Read-write memory = the same array pattern as ROM, but a **signal** you write at runtime.
- Keep both read and write inside `rising_edge(clk)` so the tool infers block RAM.
- Write-first / read-first / no-change are chosen by the order of statements in the process — pick the one your design needs; write-first is the safe default.
- Watch address width, avoid combinational reads, and don't trust BRAM initialisers across all targets.
