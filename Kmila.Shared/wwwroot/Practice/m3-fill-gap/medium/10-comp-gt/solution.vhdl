library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity cmp_gt_fill is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        gt : out STD_LOGIC
    );
end entity cmp_gt_fill;

architecture dataflow of cmp_gt_fill is
begin
    gt <= '1' when unsigned(a) > unsigned(b) else '0';
end architecture dataflow;
