-- {{PROJECT_NAME}} — generic-parameterised counter.
-- Bridges combinational-only and sequential design: synchronous register
-- with `reset` + `enable`, generic `WIDTH` so the same module covers 4-bit
-- through 32-bit counters without duplication.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity {{PROJECT_IDENT}} is
  generic (
    WIDTH : natural := 8
  );
  port (
    clk    : in  STD_LOGIC;
    reset  : in  STD_LOGIC;
    enable : in  STD_LOGIC;
    count  : out STD_LOGIC_VECTOR(WIDTH - 1 downto 0);
    carry  : out STD_LOGIC
  );
end entity {{PROJECT_IDENT}};

architecture rtl of {{PROJECT_IDENT}} is
  signal q     : unsigned(WIDTH - 1 downto 0) := (others => '0');
  signal carry_int : STD_LOGIC := '0';
begin

  process(clk, reset)
  begin
    if reset = '1' then
      q         <= (others => '0');
      carry_int <= '0';
    elsif rising_edge(clk) then
      carry_int <= '0';
      if enable = '1' then
        if q = to_unsigned(2**WIDTH - 1, WIDTH) then
          -- Wrap-around: one-cycle carry pulse so consumers can chain
          -- counters into a wider counter without external glue.
          q         <= (others => '0');
          carry_int <= '1';
        else
          q <= q + 1;
        end if;
      end if;
    end if;
  end process;

  count <= STD_LOGIC_VECTOR(q);
  carry <= carry_int;
end architecture rtl;
