library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity mux4_fill is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        c   : in  STD_LOGIC;
        d   : in  STD_LOGIC;
        sel : in  STD_LOGIC_VECTOR(1 downto 0);
        y   : out STD_LOGIC
    );
end entity mux4_fill;

architecture dataflow of mux4_fill is
begin
    -- BEGIN_EDIT
    -- TODO: route a/b/c/d to y based on sel = 00/01/10/11
    y <= '0';
    -- END_EDIT
end architecture dataflow;
