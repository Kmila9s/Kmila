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

## The three read policies

Every synchronous RAM has to decide what `dout` does **during a write**. Three conventions exist:

- **Write-first (shown above):** on a write, `dout` shows the newly-written data. Cleanest semantics — a read and a write to the same address in the same cycle give you the fresh value.
- **Read-first:** on a write, `dout` shows the **old** contents of the cell. Some CPUs' register files use this.
- **No-change:** on a write, `dout` doesn't update. Some low-power designs.

All three map to block RAM on any modern FPGA, but the synthesizer picks the mode based on the exact shape of your process. The write-first template is the most portable — if in doubt, write it.

## What the sample does

For the first 30 µs the sim writes to three addresses (`0`, `1`, `2`) with three different byte patterns. Then `we` drops, and `addr` is reset back to 0 at 40 µs. The Waveforms tab shows `dout` tracking `din` during writes, then producing the stored value back out when you read.

## Your turn

- **Upgrade to a true dual-port RAM.** Two full sets of `addr`/`din`/`dout`/`we`, both clocked by `clk`. This is the pattern every vendor's FIFOs and dual-clock DPRAMs use underneath.
- **Add an output register.** Block RAM on Xilinx supports a second pipeline register after the memory array. Add `dout_reg <= dout;` in a second clocked block and watch what changes on the waveform (hint: one more cycle of latency, but much better timing closure).
- **Change the initial contents.** Preload the first few entries with a recognisable pattern by changing the `signal mem` initialiser. Not all synthesizers respect BRAM initialisers in portable VHDL — check your target before relying on it in production.
