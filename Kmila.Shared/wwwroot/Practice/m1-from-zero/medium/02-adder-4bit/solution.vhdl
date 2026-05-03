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
    signal s : unsigned(4 downto 0);
begin
    s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
    sum  <= std_logic_vector(s(3 downto 0));
    cout <= s(4);
end architecture behavioral;
