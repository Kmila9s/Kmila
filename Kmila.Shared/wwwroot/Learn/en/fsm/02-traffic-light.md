# Sample: traffic light FSM

A traffic light is the canonical Moore FSM: three states (`RED`, `GREEN`, `YELLOW`), one output per colour, and a timer that says "hold each state for a few cycles before moving on."

## The structure

The sample follows the **two-process pattern**:

- Process 1 is the **state register** — a classic D flip-flop that stores `state` and ticks a hold counter.
- Process 2 is the **next-state logic** — pure combinational, decides what `next_state` is given the current one.
- The output assignments (bottom of the architecture) are combinational Moore outputs — their values depend only on `state`.

## Walk the code

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 ticks per colour
```

`state_t` is an **enumeration** — the synthesizer picks the actual binary encoding, we just refer to named states. `count` tracks how long we've been in the current state. `HOLD` is the dwell time.

State register:

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    state <= S_RED;
    count <= (others => '0');
  elsif rising_edge(clk) then
    if count = HOLD then
      state <= next_state;
      count <= (others => '0');
    else
      count <= count + 1;
    end if;
  end if;
end process;
```

Two details worth naming. First, this is an **asynchronous reset** (`reset` is in the sensitivity list and tested before `rising_edge`) — the light snaps to `S_RED` the instant reset asserts. A synchronous reset would drop `reset` from the sensitivity list and test it only inside the `rising_edge` branch; both are valid, just pick one style and stay consistent. Second, the counter turns one state transition into an 8-cycle dwell — the state only advances on the cycle where `count = HOLD`.

Next-state logic:

```vhdl
process(state)
begin
  case state is
    when S_RED    => next_state <= S_GREEN;
    when S_GREEN  => next_state <= S_YELLOW;
    when S_YELLOW => next_state <= S_RED;
  end case;
end process;
```

Because this process assigns `next_state` on every branch and the `case` is exhaustive over `state_t`, there's no inferred latch here. The sensitivity list is just `(state)` — correct, because `next_state` depends on nothing else. The moment you make the next state depend on an input, that input has to join the sensitivity list (or the combinational process will simulate wrong), and you should add a `next_state <= state;` default on the first line as insurance.

Outputs (pure Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

Each output is a function of `state` alone — so they only change when the register ticks, and they can't glitch between edges. That's the whole appeal of a Moore machine for something that drives a physical signal.

## What the sample does

The clock runs for about 400 µs. Every 8 clocks (7 hold + 1 transition), the FSM advances. You'll see three square waves rotating in sequence: `red` goes high for 8 ticks, then `green` for 8 ticks, then `yellow`, then back to `red`.

## From simulation to a real crossing

`HOLD = 7` is a simulation convenience. On a 50 MHz FPGA, one tick is 20 ns, so seven ticks is 140 ns — not a traffic light, a strobe. For a real 1-second dwell you'd count to 50 000 000, which means widening `count` and the `HOLD` constant. The clean way is a **generic**:

```vhdl
generic ( HOLD : natural := 7 );
...
signal count : unsigned(31 downto 0);
```

Simulate with a tiny `HOLD`, synthesize with a big one — same RTL, no edits. Real intersections also add an **all-red safety state** between conflicting greens; adding one is a good exercise in extending a Moore machine without breaking its glitch-free guarantee.

## Your turn

- **Add a pedestrian-request input.** When `ped = '1'` and the state is `S_GREEN`, force the next state to `S_YELLOW` (so the cycle shortens to let pedestrians cross). This turns the Moore machine into something Mealy-ish — the next state now depends on both `state` **and** the `ped` input, so add `ped` to the sensitivity list and a default assignment.
- **Emit a `walk` signal** that's high only when `state = S_RED`. That's a new Moore output.
- **Parameterise `HOLD`** as a generic — 7 is fine for simulation, but on a 50 MHz FPGA you'd want 50 million cycles to get a 1 s hold.

## Ties to the course

This sample is Unit II's *aplicaciones con autómatas y lenguaje de descripción de hardware* in miniature: a deterministic Moore automaton, drawn as a three-state diagram, then described in HDL. The counter that stretches each state into a timed dwell is also your first taste of Unit III (contadores) living inside a state machine.

## What to remember

- The traffic light is a textbook Moore FSM: outputs depend only on `state`, so they're glitch-free.
- The two-process split (clocked register + combinational next-state) keeps the design readable and latch-free.
- A counter inside the state register turns one transition into a timed dwell; parameterise the dwell with a generic so one RTL serves both sim and silicon.
- The moment an input influences the next state, add it to the sensitivity list and default-assign `next_state`.
