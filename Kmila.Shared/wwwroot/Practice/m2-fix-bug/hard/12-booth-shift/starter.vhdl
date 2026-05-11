library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity booth_shift is
    port (
        clk     : in  STD_LOGIC;
        reset   : in  STD_LOGIC;
        a       : in  STD_LOGIC_VECTOR(7 downto 0);
        b       : in  STD_LOGIC_VECTOR(2 downto 0);
        product : out STD_LOGIC_VECTOR(15 downto 0)
    );
end entity booth_shift;

architecture behavioral of booth_shift is
    signal acc : signed(15 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            acc <= (others => '0');
        elsif rising_edge(clk) then
            -- BUG: uses LOGICAL right-shift (zero-fill) instead of ARITHMETIC
            -- right-shift (sign-fill). Negative accumulator values lose the sign bit.
            if b(0) = '1' then
                acc <= signed(shift_right(unsigned(acc + signed(resize(signed(a), 16))), 1));
            else
                acc <= signed(shift_right(unsigned(acc), 1));
            end if;
        end if;
    end process;
    product <= std_logic_vector(acc);
end architecture behavioral;
