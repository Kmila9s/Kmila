# The testbench mindset

In every lesson up to here, Kmila has been your testbench — you dragged stimulus into the Ports tab, Run ran the design, and you eyeballed the waveform. That's fine for learning. It's not how professional digital design works.

## What a real testbench is

A **testbench** is another VHDL entity whose sole job is to exercise a *design under test* (DUT). It has no ports, because nothing's driving it; it declares the DUT as a component, wires its own internal signals to the DUT's ports, and then uses a **process** to walk through stimulus sequences in time.

Skeleton:

```vhdl
entity tb_counter is
end entity tb_counter;

architecture sim of tb_counter is
    component counter
        port ( clk, reset : in  std_logic;
               q          : out std_logic_vector(3 downto 0) );
    end component;

    signal clk   : std_logic := '0';
    signal reset : std_logic := '1';
    signal q     : std_logic_vector(3 downto 0);
begin
    dut : counter port map (clk, reset, q);

    clk <= not clk after 10 ns;            -- 50 MHz

    process
    begin
        wait for 20 ns;
        reset <= '0';
        wait for 200 ns;
        assert q = "1010"
            report "Counter should be 10 at 200 ns, got " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

Three new ideas:

1. **`wait`** — time passes only when a process says so.
2. **`assert … report … severity`** — the self-check. Turns a 1000-line waveform into "pass" or "fail."
3. **Zero-port entity** — testbenches are self-contained; the simulator instantiates them directly.

## Why this matters

- **Regression**: a testbench you wrote yesterday still runs today. A waveform you stared at yesterday is gone.
- **Coverage**: you can drive stimulus patterns an interactive session never reaches (random inputs, edge cases, corner frequencies).
- **Automation**: a CI system runs your testbench on every commit. Waveforms don't fit in CI.

## Kmila's current limits

The Kmila simulator runs testbenches just fine if you write them as regular entities — the only thing missing today is automated pass/fail extraction from `assert` statements into the Output log. That's on the roadmap.

*More lessons coming — this module is under active authoring.*
