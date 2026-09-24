library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity add2_buggy is
    port (
        a    : in  STD_LOGIC_VECTOR(1 downto 0);
        b    : in  STD_LOGIC_VECTOR(1 downto 0);
        cin  : in  STD_LOGIC;
        sum  : out STD_LOGIC_VECTOR(1 downto 0);
        cout : out STD_LOGIC
    );
end entity add2_buggy;

architecture behavioral of add2_buggy is
    signal tmp : unsigned(2 downto 0);
begin
    tmp  <= ("0" & unsigned(a)) + ("0" & unsigned(b)) + ("00" & cin);
    sum  <= STD_LOGIC_VECTOR(tmp(1 downto 0));
    cout <= tmp(2);
end architecture behavioral;
