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

Outputs (pure Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## What the sample does

The clock runs for about 400 µs. Every 8 clocks (7 hold + 1 transition), the FSM advances. You'll see three square waves rotating in sequence: `red` goes high for 8 ticks, then `green` for 8 ticks, then `yellow`, then back to `red`.

## Your turn

- **Add a pedestrian-request input.** When `ped = '1'` and the state is `S_GREEN`, force the next state to `S_YELLOW` (so the cycle shortens to let pedestrians cross). This turns the Moore machine into something Mealy-ish — the output `yellow` depends on both `state` **and** the `ped` input.
- **Emit a `walk` signal** that's high only when `state = S_RED`. That's a new Moore output.
- **Parameterise `HOLD`** as a generic — 7 is fine for simulation, but on a 50 MHz FPGA you'd want a few hundred million cycles to get a 1 s hold.
