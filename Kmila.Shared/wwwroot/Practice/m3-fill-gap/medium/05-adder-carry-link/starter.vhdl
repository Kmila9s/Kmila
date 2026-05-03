library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity adder2_carry_fill is
    port (
        a    : in  STD_LOGIC_VECTOR(1 downto 0);
        b    : in  STD_LOGIC_VECTOR(1 downto 0);
        cin  : in  STD_LOGIC;
        sum  : out STD_LOGIC_VECTOR(1 downto 0);
        cout : out STD_LOGIC
    );
end entity adder2_carry_fill;

architecture dataflow of adder2_carry_fill is
    signal c1 : STD_LOGIC;
begin
    -- Bit-0 sum (locked)
    sum(0) <= a(0) xor b(0) xor cin;

    -- BEGIN_EDIT
    -- TODO: drive c1 with the carry-out of the bit-0 full adder
    --       (using a(0), b(0), cin)
    c1 <= '0';
    -- END_EDIT

    -- Bit-1 sum and final carry-out (locked)
    sum(1) <= a(1) xor b(1) xor c1;
    cout   <= (a(1) and b(1)) or (c1 and (a(1) xor b(1)));
end architecture dataflow;
