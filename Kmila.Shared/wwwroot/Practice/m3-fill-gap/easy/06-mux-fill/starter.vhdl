library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux_fill is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        sel : in  STD_LOGIC;
        y   : out STD_LOGIC
    );
end entity mux_fill;

architecture behavioral of mux_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: y = a when sel='0' else b
    -- END_EDIT
end architecture behavioral;
