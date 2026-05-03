library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity booth_step_fill is
    port (
        clk     : in  STD_LOGIC;
        reset   : in  STD_LOGIC;
        start   : in  STD_LOGIC;
        a       : in  STD_LOGIC_VECTOR(3 downto 0);
        b       : in  STD_LOGIC_VECTOR(3 downto 0);
        product : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity booth_step_fill;

architecture behavioral of booth_step_fill is
    signal acc      : signed(7 downto 0) := (others => '0');
    signal q0       : STD_LOGIC := '0';
    signal q_minus1 : STD_LOGIC := '0';
    signal m_ext    : signed(7 downto 0) := (others => '0');
begin
    process(clk, reset)
        variable lsb_pair : STD_LOGIC_VECTOR(1 downto 0);
    begin
        if reset = '1' then
            acc      <= (others => '0');
            q0       <= '0';
            q_minus1 <= '0';
            m_ext    <= (others => '0');
        elsif rising_edge(clk) then
            if start = '1' then
                acc      <= (others => '0');
                q0       <= b(0);
                q_minus1 <= '0';
                m_ext    <= resize(signed(a), 8);
            else
                lsb_pair := q0 & q_minus1;
                -- BEGIN_EDIT
                case lsb_pair is
                    when "01" =>
                        acc <= acc + m_ext;
                    when "10" =>
                        acc <= acc - m_ext;
                    when others =>
                        acc <= acc;
                end case;
                -- END_EDIT
            end if;
        end if;
    end process;

    product <= std_logic_vector(acc);
end architecture behavioral;
