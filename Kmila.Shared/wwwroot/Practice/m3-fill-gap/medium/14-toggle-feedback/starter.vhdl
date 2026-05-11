library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity toggle_fb_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        en    : in  STD_LOGIC;
        q     : out STD_LOGIC
    );
end entity toggle_fb_fill;

architecture behavioral of toggle_fb_fill is
    signal qi : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            qi <= '0';
        elsif rising_edge(clk) then
            if en = '1' then
                -- BEGIN_EDIT
                qi <= qi;  -- TODO: invert qi when en=1
                -- END_EDIT
            end if;
        end if;
    end process;
    q <= qi;
end architecture behavioral;
