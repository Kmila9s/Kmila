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

## Acceptance criteria

| Coin sequence        | Expected behavior             |
|----------------------|-------------------------------|
| 5 nickels            | `dispense=1, change=0`        |
| 2 dimes + 1 nickel   | `dispense=1, change=0`        |
| 3 dimes (= 30¢)      | `dispense=1, change=1`        |
| 2 nickels + 1 dime   | no dispense (only 20¢)        |

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
2. **Pulse `dispense` for exactly one cycle.** The trick: when you detect the sale, set a flag signal and reset `total` back to zero. The flag lives for one cycle.
3. **Compute `change`.** If the total at the sale moment was > 25, pulse `change` too.

Hint: it's easier if you compute the "next total" as a local signal (or `variable` inside the process):

```vhdl
variable next_total : unsigned(5 downto 0);
...
next_total := total;
if    nickel = '1' then next_total := total + 5;
elsif dime   = '1' then next_total := total + 10;
end if;

if next_total >= 25 then
  dispense <= '1';
  change   <= '1' when next_total > 25 else '0';
  total    <= (others => '0');
else
  dispense <= '0';
  change   <= '0';
  total    <= next_total;
end if;
```

## Going further

- Add a **refund** button that zeros out `total` without dispensing.
- Support a **quarter** (25¢) coin — should vend immediately.
- Track the number of items sold in a separate 8-bit counter visible as an output. This is the kind of feedback real vending machines send upstream.
