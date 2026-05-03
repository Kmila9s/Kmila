library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity comparator_gle is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        gt : out STD_LOGIC;
        eq : out STD_LOGIC;
        lt : out STD_LOGIC
    );
end entity comparator_gle;

architecture behavioral of comparator_gle is
begin
    -- TODO: drive gt, eq, lt by comparing unsigned(a) and unsigned(b).
    gt <= '0';
    eq <= '0';
    lt <= '0';
end architecture behavioral;
