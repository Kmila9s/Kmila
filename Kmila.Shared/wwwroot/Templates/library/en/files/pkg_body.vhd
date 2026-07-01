-- {{PROJECT_NAME}} — VHDL library package body.
-- Implementations for the declarations in pkg.vhd. When Kmila imports
-- this library, both files are concatenated so the package and body
-- compile together.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

package body {{PROJECT_IDENT}} is

  function sat_add(a : data_bus_t; b : data_bus_t) return data_bus_t is
    variable wide : unsigned(DATA_WIDTH downto 0);
  begin
    wide := resize(unsigned(a), DATA_WIDTH + 1) +
            resize(unsigned(b), DATA_WIDTH + 1);
    if wide(DATA_WIDTH) = '1' then
      -- Overflow: clamp to all-ones so consumers see a clean saturation.
      return (others => '1');
    else
      return STD_LOGIC_VECTOR(wide(DATA_WIDTH - 1 downto 0));
    end if;
  end function sat_add;

  function popcount(v : data_bus_t) return unsigned is
    variable acc : unsigned(3 downto 0) := (others => '0');
  begin
    for i in v'range loop
      if v(i) = '1' then
        acc := acc + 1;
      end if;
    end loop;
    return acc;
  end function popcount;

end package body {{PROJECT_IDENT}};
