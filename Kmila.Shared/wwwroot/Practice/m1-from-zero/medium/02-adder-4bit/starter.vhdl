library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity adder4 is
    port (
        a    : in  STD_LOGIC_VECTOR(3 downto 0);
        b    : in  STD_LOGIC_VECTOR(3 downto 0);
        cin  : in  STD_LOGIC;
        sum  : out STD_LOGIC_VECTOR(3 downto 0);
        cout : out STD_LOGIC
    );
end entity adder4;

architecture behavioral of adder4 is
    -- TODO: declare a 5-bit unsigned signal to capture sum + carry-out.
begin
    -- TODO: compute sum and cout using numeric_std.
    sum  <= (others => '0');
    cout <= '0';
end architecture behavioral;
