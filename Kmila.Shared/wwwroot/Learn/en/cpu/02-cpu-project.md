# Project: single-cycle 8-bit CPU

This is the capstone. You'll assemble everything from the earlier modules — a ROM, a register, an ALU, an FSM-ish control — into a tiny machine that actually runs programs.

## The machine

```mermaid
flowchart TD
    RST(["reset = 0000"]) -.-> PC["PC (4 bits)"]
    PC -- addr --> ROM["ROM<br/>16 × 8 bytes<br/>(preloaded at synth)"]
    ROM -- "instr (8 bits)" --> SPLIT{{"opcode = instr(7..4)<br/>imm4 = instr(3..0)"}}
    SPLIT --> DECEXE["decode / execute"]
    SPLIT --> PCNXT["PC next logic"]
    DECEXE --> R0["R0 (8 bits)"]
    PCNXT --> PCN["PC_next"]
```

## What the scaffold gives you

A complete, working CPU. Read the starter carefully:

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    pc   <= (others => '0');
    r0   <= (others => '0');
    halt <= '0';
  elsif rising_edge(clk) then
    if halt = '0' then
      case instr(7 downto 4) is
        when "0000" => r0 <= resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0001" => r0 <= r0 + resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0010" => pc <= unsigned(instr(3 downto 0));
        when "1111" => halt <= '1';
        when others => pc <= pc + 1;
      end case;
    end if;
  end if;
end process;
```

It already implements:

- LOAD (`0000`)
- ADD  (`0001`)
- JUMP (`0010`)
- HALT (`1111`)

The preloaded ROM runs `R0 = 3; R0 += 4; R0 += 2; HALT`.

## Your job

Run it as-is first. Simulate for 200 µs with the bundled clock + reset stimulus. On the Waveforms tab you should see `r0_out` step through `3 → 7 → 9`, then `halted` go high and stay high.

Once the baseline works, **extend** the ISA:

### Task 1: SUB

Opcode `0011`. `R0 <= R0 - imm4`. One-line change.

### Task 2: Conditional jump

Opcode `0100`: `JZ addr` — jump to `addr` if `R0 = 0`, otherwise just `pc <= pc + 1`.

Write a small program that counts down from some value and jumps back to 0 when done. Use `JZ` as the loop exit.

### Task 3: A second register

Add `r1` as a second 8-bit register. Define `LOAD R1, #i` as opcode `0101` and `ADD R0, R1` (no immediate) as opcode `0110`. You'll need to widen the decode case and add a second output port for visibility.

### Task 4: External data memory

Hook the RAM module from the Memories track into this CPU. Add `LOAD R0, [addr]` and `STORE R0, [addr]` opcodes. Careful: a single-cycle design requires the memory's read port to have its output valid within the same cycle you issue the address — use a combinational read for the ROM and a registered read for the data RAM, and plan the pipeline carefully.

## Why do this?

Because the first time you watch your CPU step through a program you wrote (in bytes you wrote into a ROM), something clicks. Every abstraction on top — assembler, compiler, operating system — stands on exactly this mechanism. And every one of them, all the way up to the web browser running the JavaScript you wrote last week, is the same trick repeated at larger scale.

## Going further

- Implement the classic five-stage pipeline on this same ISA. Add hazard detection and a forwarding unit. You now have a microscale version of what RISC-V textbooks teach.
- Retarget the ROM: instead of constants, load it from an external file at simulation time, then build a toolchain that assembles text into those bytes.
- Drop in the UART TX from the earlier project and make the CPU print `'HI'` to the waveform. Congratulations — you have a SoC.
