-- {{PROJECT_NAME}} — VHDL library package declaration.
-- Exposes constants, types, and helper functions for other Kmila projects
-- to `use` via `use {{PROJECT_IDENT}}.<symbol>.all;`.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

package {{PROJECT_IDENT}} is

  -- Width used by the helper bus type. Bump as needed when consuming
  -- the library — it's part of the public surface.
  constant DATA_WIDTH : natural := 8;

  -- Convenience subtype so consumers don't have to repeat the width.
  subtype data_bus_t is STD_LOGIC_VECTOR(DATA_WIDTH - 1 downto 0);

  -- Saturating add: returns DATA_WIDTH-bit sum, clamped to all-ones on overflow.
  function sat_add(a : data_bus_t; b : data_bus_t) return data_bus_t;

  -- Population count: returns the number of '1' bits as a small unsigned.
  function popcount(v : data_bus_t) return unsigned;

end package {{PROJECT_IDENT}};
