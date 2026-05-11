library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity rr_token_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        token : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity rr_token_fill;

architecture behavioral of rr_token_fill is
    signal tok : STD_LOGIC_VECTOR(3 downto 0) := "0001";
begin
    process(clk, reset)
    begin
        if reset = '1' then
            tok <= "0001";
        elsif rising_edge(clk) then
            -- BEGIN_EDIT
            tok <= tok;  -- TODO: rotate token left by one
            -- END_EDIT
        end if;
    end process;
    token <= tok;
end architecture behavioral;
