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

## Generating a clock

That one-liner, `clk <= not clk after 10 ns;`, is the whole clock: it flips every 10 ns, giving a 20 ns period (50 MHz). Because `clk` has an initial value of `'0'`, it starts cleanly. For a non-50%-duty clock or one you need to stop later, use a process instead:

```vhdl
clk_gen : process
begin
    clk <= '0'; wait for 10 ns;
    clk <= '1'; wait for 10 ns;
    if done then wait; end if;   -- freeze when the test is over
end process;
```

Keep clock generation and stimulus in **separate** processes. The clock just ticks; the stimulus process drives inputs and checks outputs, usually synchronising with `wait until rising_edge(clk)` so it lines up with the DUT.

## Self-checking: assert, severity, and golden values

The point of a testbench is that it checks *itself*. `assert` evaluates a condition and, if it's false, prints the `report` string at the chosen `severity`. The four severity levels are **note**, **warning**, **error**, and **failure** — and most simulators can be told to halt the moment one of a given level fires. That's how you turn "it looked right" into a hard pass/fail.

For non-trivial designs, don't hand-write every expected value. Compute it from a **golden model** — a simple, obviously-correct reference (even a plain software-style calculation in the testbench) — and compare the DUT against it on every cycle. That comparison-against-a-reference is the core idea behind a **scoreboard**, and it scales from a counter to a CPU.

```vhdl
expected <= std_logic_vector(unsigned(expected) + 1) when rising_edge(clk);
assert q = expected
    report "mismatch: got " & to_string(q) & " expected " & to_string(expected)
    severity error;
```

(`to_string` for `std_logic_vector` is a VHDL-2008 convenience — compile the testbench in 2008 mode to use it.)

## Ending the simulation cleanly

A waveform-only session just runs forever. A self-checking testbench should **stop itself** when the stimulus is exhausted. In VHDL-2008 the clean way is `finish` from the `std.env` package:

```vhdl
use std.env.all;
...
report "All tests passed";
finish;          -- ends the simulation; stop would pause for inspection
```

`finish` ends the run (ideal for automated/CI use); `stop` pauses it so you can poke around interactively. The older trick of forcing an `assert … severity failure` to halt also works in any VHDL version, but it reports a *failure* even on a passing run, which confuses regression tools — prefer `finish`.

## Directed vs. randomized stimulus

- **Directed** tests hit specific, hand-chosen cases: reset behaviour, the rollover of a counter, a known tricky sequence. Great for pinning down a bug and for the acceptance cases in a spec.
- **Randomized / constrained-random** tests throw many legal-but-unplanned inputs at the DUT, catching corners you'd never think to write by hand. They lean on the golden model, because you can't hand-predict every result.

Most real verification mixes both: directed tests for the cases you know matter, randomized for the ones you don't.

## Why this matters

- **Regression:** a testbench you wrote yesterday still runs today. A waveform you stared at yesterday is gone.
- **Coverage:** you can drive stimulus patterns an interactive session never reaches (random inputs, edge cases, corner frequencies).
- **Automation:** a CI system runs your testbench on every commit and reads the pass/fail from the exit. Waveforms don't fit in CI.

## Ties to the course

Verification is the quiet backbone of the whole *Diseño de Sistemas Digitales* lab sequence. Every practice — registers, sequential systems, counters, memories, ASM machines — is really "build it in HDL, then prove it works." A self-checking testbench is how you turn each *reporte de práctica* from "here's a screenshot of a waveform" into "here's a test that passes," which is exactly the rigor the unit's problem-based evaluation is after.

## Kmila's current limits

The Kmila simulator runs testbenches just fine if you write them as regular entities — the only thing missing today is automated pass/fail extraction from `assert` statements into the Output log. That's on the roadmap.

## What to remember

- A testbench is a port-less entity that instantiates the DUT, generates a clock, drives stimulus, and **checks itself** with `assert`.
- Keep clock and stimulus in separate processes; synchronise checks with `wait until rising_edge(clk)`.
- Compare against a golden model (a scoreboard) instead of hand-writing every expected value.
- End automated runs with `std.env.finish`; use severity levels so the simulator can halt on real failures.
- Mix directed tests (known cases) with randomized ones (the corners you'd never guess).
