-- {{PROJECT_NAME}} — Moore-FSM canonical pattern.
-- Two processes: combinational next-state logic + synchronous state register.
-- Add states to the enum, edit next-state transitions, and update the
-- output assignment at the bottom.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity {{PROJECT_IDENT}} is
  port (
    clk     : in  STD_LOGIC;
    reset   : in  STD_LOGIC;
    trigger : in  STD_LOGIC;
    done    : out STD_LOGIC
  );
end entity {{PROJECT_IDENT}};

architecture moore of {{PROJECT_IDENT}} is

  type state_t is (S_IDLE, S_WORK, S_DONE);
  signal current_state : state_t := S_IDLE;
  signal next_state    : state_t := S_IDLE;

begin

  -- State register: stores the current FSM state.
  state_reg: process(clk, reset)
  begin
    if reset = '1' then
      current_state <= S_IDLE;
    elsif rising_edge(clk) then
      current_state <= next_state;
    end if;
  end process state_reg;

  -- Next-state logic: pure combinational, depends only on inputs + state.
  next_logic: process(current_state, trigger)
  begin
    -- Default: stay in the current state. Each case overrides as needed.
    next_state <= current_state;

    case current_state is
      when S_IDLE =>
        if trigger = '1' then
          next_state <= S_WORK;
        end if;

      when S_WORK =>
        next_state <= S_DONE;

      when S_DONE =>
        next_state <= S_IDLE;

      when others =>
        next_state <= S_IDLE;
    end case;
  end process next_logic;

  -- Moore output: depends only on the current state.
  done <= '1' when current_state = S_DONE else '0';

end architecture moore;
