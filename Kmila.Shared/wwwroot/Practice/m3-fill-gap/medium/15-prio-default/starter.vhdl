library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity prio_default_fill is
    port (
        d : in  STD_LOGIC_VECTOR(3 downto 0);
        y : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity prio_default_fill;

architecture behavioral of prio_default_fill is
begin
    process(d)
    begin
        if d(3) = '1' then
            y <= "11";
        elsif d(2) = '1' then
            y <= "10";
        elsif d(1) = '1' then
            y <= "01";
        elsif d(0) = '1' then
            y <= "00";
        else
            -- BEGIN_EDIT
            y <= "11";  -- TODO: pick the all-zero default
            -- END_EDIT
        end if;
    end process;
end architecture behavioral;
