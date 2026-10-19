# Project: coin-operated vending machine

This is your first proper project. The spec is fixed, the starter code hands you the plumbing, and your job is to write the logic that completes it.

## What you're building

An FSM that tracks the money a customer has inserted and vends a product (a single output pulse, `dispense`) when the total reaches 25¢. The inputs are:

- `nickel` — a 1-cycle pulse meaning "a 5¢ coin just dropped in"
- `dime`   — a 1-cycle pulse meaning "a 10¢ coin just dropped in"
- `clk`, `reset`

And the outputs:

- `dispense` — pulse high for exactly one clock cycle when you've reached ≥ 25¢
- `change`   — pulse high for one cycle alongside `dispense` if the total was **more** than 25¢ (so the machine needs to return money)

## Two ways to model this

A vending machine is a classic FSM, and there are two honest ways to describe it:

- **Explicit states.** Enumerate every reachable total — `S0, S5, S10, S15, S20` — and draw a transition for each coin. This is the textbook approach, and it's what you'd do by hand for a small coin set. It makes the state diagram obvious but explodes if you add more coins or a higher price.
- **Accumulator (datapath) style.** Keep the total in a small register and compare against the price. Fewer lines, scales to any price, and it's the shape the starter code uses. You can think of the `total` register as the state and the comparator as the next-state logic.

Either way, `dispense` depends on the coin arriving *this* cycle, so it behaves like a **Mealy** output — it reacts in the same cycle the sale condition becomes true, rather than waiting a cycle.

## What the starter gives you

The entity and `total` accumulator are wired up. Every `nickel` pulse adds 5; every `dime` pulse adds 10. But the starter always drives `dispense = '0'` and `change = '0'`. **That's what you need to change.**

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    total <= (others => '0');
  elsif rising_edge(clk) then
    if    nickel = '1' then total <= total + 5;
    elsif dime   = '1' then total <= total + 10;
    end if;
  end if;
end process;

dispense <= '0';  -- TODO
change   <= '0';  -- TODO
```

## Suggested approach

1. **Detect the sale condition.** On every rising clock edge, check if `total >= 25` after adding the current coin. (Careful: you need to compute the *new* total, not the one from the previous cycle.)
2. **Pulse `dispense` for exactly one cycle.** The trick: when you detect the sale, set the pulse and reset `total` back to zero. Because `total` is cleared on the same edge, the condition is false next cycle, so the pulse is naturally one cycle wide.
3. **Compute `change`.** If the total at the sale moment was > 25, pulse `change` too.

Compute the "next total" as a `variable` inside the process so the decision uses the up-to-date value, not last cycle's register:

```vhdl
process(clk, reset)
  variable next_total : unsigned(5 downto 0);
begin
  if reset = '1' then
    total    <= (others => '0');
    dispense <= '0';
    change   <= '0';
  elsif rising_edge(clk) then
    next_total := total;
    if    nickel = '1' then next_total := total + 5;
    elsif dime   = '1' then next_total := total + 10;
    end if;

    if next_total >= 25 then
      dispense <= '1';
      if next_total > 25 then change <= '1'; else change <= '0'; end if;
      total    <= (others => '0');
    else
      dispense <= '0';
      change   <= '0';
      total    <= next_total;
    end if;
  end if;
end process;
```

Two things to notice. A `variable` is assigned with `:=` and updates **immediately** inside the process, which is exactly why `next_total` already includes this cycle's coin — a signal (`<=`) wouldn't update until the process suspends. And because `dispense`, `change`, and `total` are all driven inside the clocked process, they're **registered** — the pulses line up with clock edges and won't glitch.

## Watch out for

- **Width.** `total + 10` can reach 30 before the reset fires, so size the register for at least 6 bits — `unsigned(5 downto 0)` holds 0–63 with room to spare. Too-narrow a vector wraps silently.
- **Both coins at once.** The `if … elsif` means a simultaneous `nickel` and `dime` only counts the nickel. Decide whether that's acceptable or an error for your spec.
- **Forgetting to clear `total`.** If you pulse `dispense` but don't zero the accumulator, the machine "sells" again every cycle until more coins change the total. Clearing on the sale edge is what makes the pulse one cycle wide.

## Acceptance criteria

| Coin sequence        | Expected behavior             |
|----------------------|-------------------------------|
| 5 nickels            | `dispense=1, change=0`        |
| 2 dimes + 1 nickel   | `dispense=1, change=0`        |
| 3 dimes (= 30¢)      | `dispense=1, change=1`        |
| 2 nickels + 1 dime   | no dispense (only 20¢)        |

## Going further

- Add a **refund** button that zeros out `total` without dispensing.
- Support a **quarter** (25¢) coin — should vend immediately.
- Track the number of items sold in a separate 8-bit counter visible as an output. This is the kind of feedback real vending machines send upstream.

## Ties to the course

This is Unit II's *aplicaciones con autómatas y lenguaje de descripción de hardware*: a deterministic sequential machine specified from a behaviour, then described in HDL. The accumulator-plus-comparator shape is also a gentle lead-in to Unit V, where you'll split a design explicitly into a **datapath** (the `total` register and adder) and a **control** automaton that drives it.

## What to remember

- A vending machine is an FSM; model it with explicit states for clarity, or an accumulator register for scale.
- Use a `variable` (`:=`) for the "next total" so the sale check includes the coin that just arrived.
- Make a one-cycle pulse by clearing the accumulator on the sale edge — registered outputs keep the pulse clean.
- Size vectors for the largest value you can reach, and decide up front how simultaneous inputs behave.
