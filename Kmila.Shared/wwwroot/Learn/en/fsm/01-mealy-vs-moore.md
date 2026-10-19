# Mealy vs Moore machines

A finite-state machine (FSM) is control logic that remembers what it's currently doing. Formally it's a deterministic automaton: a finite set of states, a transition function that picks the next state from the current state and inputs, and an output function. Every synchronous FSM you build has three parts:

- A **state register** (a group of flip-flops) — the only thing with memory.
- **Next-state logic** (combinational, decides what the state will be on the next clock edge).
- **Output logic** (combinational, decides what the outputs are right now).

Where the output logic looks for its inputs splits FSMs into two families.

## Moore: outputs depend on state only

```mermaid
flowchart LR
    IN([input]) --> NS["next-state"]
    NS --> SR["state reg"]
    SR --> OUT["outputs"]
```

Given a state, the output is fixed. If you're in `S_GREEN`, `green = '1'`. Always. Regardless of what the input looks like.

**Pros**
- Outputs only change at clock edges → no glitches.
- Easy to reason about, easy to debug, easy to verify.

**Cons**
- At least one clock cycle of latency between input and output (the state has to transition first).
- Sometimes you need more states to encode the same behavior as a Mealy machine.

## Mealy: outputs depend on state + input

```mermaid
flowchart LR
    IN([input]) --> NS["next-state"]
    NS --> SR["state reg"]
    IN --> OL["output logic"]
    SR --> OL
    OL --> OUT([outputs])
```

Outputs are a function of the current state **and** the current input. They can change between clock edges.

**Pros**
- Faster — outputs can react to an input in the same cycle, not the next.
- Often needs fewer states.

**Cons**
- Outputs can glitch (they follow the combinational input).
- Harder to debug when outputs don't line up with clock edges.

A quick way to feel the difference: the same "detect a `1` on the input" job is one state in Mealy (assert the output on the transition that *sees* the `1`) but needs an extra "I just saw a 1" state in Moore (so the output can be tied to being *in* that state). Same behaviour, different state count, one cycle apart in timing.

## One, two, or three processes

VHDL gives you a few ways to write the same machine. They synthesise to the same hardware; they differ in readability and in how easy they are to get wrong.

- **One-process:** state register *and* outputs live in one clocked process. Everything ends up registered, so outputs are naturally glitch-free (Moore-style, registered). Compact, but the next-state and output logic are tangled together.
- **Two-process (recommended default):** one clocked process holds the state register; one combinational process computes `next_state` and the outputs. This is the pattern the Kmila samples use — it maps cleanly onto the three-part picture above.
- **Three-process:** state register, next-state logic, and output logic each get their own process. The most verbose, but the clearest separation and the hardest to accidentally break.

## The one pitfall that bites everyone: inferred latches

In a *combinational* process, every signal you drive must get a value on **every** path through the process. If one branch of a `case` or `if` forgets to assign `next_state` (or an output), synthesis has to *remember* the old value — and the only thing that remembers in combinational logic is a **latch**. Latches are almost never what you want: they're level-sensitive, hard to time, and a classic source of "works in sim, fails on silicon."

The fix is a one-liner: assign a **default** to every signal at the top of the process, before the `case`.

```vhdl
process(state, input)
begin
  next_state <= state;   -- default: stay put
  green      <= '0';     -- default every output, too
  case state is
    when S_IDLE  => if input = '1' then next_state <= S_RUN; end if;
    when S_RUN   => green <= '1';
                    next_state <= S_IDLE;
  end case;
end process;
```

Now even if a branch "forgets" an assignment, the default covers it — no latch. (Note that VHDL requires `case` choices to be exhaustive, so you'll often add `when others =>`; that satisfies the compiler but the *default assignment* is what actually saves you from latches.)

## State encoding

You write named states with an enumerated type and let the tool choose the bits:

```vhdl
type state_t is (S_IDLE, S_RUN, S_DONE);
signal state, next_state : state_t;
```

The synthesizer picks an encoding — **binary** (fewest flip-flops), **one-hot** (one flip-flop per state, fast and FPGA-friendly), or **Gray** (one bit changes per step, good for low-noise transitions). On FPGAs one-hot is often the default because flip-flops are plentiful and it keeps the next-state logic shallow. You rarely need to hand-encode; let the tool decide unless you have a reason not to.

## Which one should you use?

**Default: Moore.** The glitch-freeness and edge-aligned outputs make designs predictable. For control logic inside a CPU, a memory controller, or anything safety-relevant, Moore is the safer choice.

**Reach for Mealy when** you need to react within the same clock cycle, or when the extra Moore states would balloon the design. UART and protocol engines often end up Mealy for the reaction-time argument. If you like Mealy's state count but want Moore's clean timing, **register the Mealy outputs** (run them through one more flip-flop) — you get glitch-free outputs back, at the cost of the one cycle of latency you were trying to avoid.

## Ties to the course

This is the heart of Unit II, *Diseño de sistemas digitales*: the deterministic automaton with and without output, the Mealy and Moore models, and their description in HDL. On paper you'll also build these from JK / SR / T / D flip-flops and shrink them with state-reduction (prime-implicant / tabular) methods — the enumerated-type style here is the HDL counterpart of that same state diagram.

## What to remember

- Moore: output is a function of state alone. Glitch-free, one cycle of latency.
- Mealy: output is a function of state + input. Faster, can glitch — register the outputs if that matters.
- Use the two-process pattern, and **default every signal** at the top of the combinational process to avoid inferred latches.
- Let the tool pick the state encoding (one-hot is common on FPGAs); start with Moore and move to Mealy for a specific reason.
