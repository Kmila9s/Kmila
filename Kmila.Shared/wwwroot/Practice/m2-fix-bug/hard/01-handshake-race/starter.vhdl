library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity handshake_race is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        req   : in  STD_LOGIC;
        ack   : out STD_LOGIC
    );
end entity handshake_race;

architecture behavioral of handshake_race is
begin
    -- BUG: ack tracks req combinationally — a one-cycle req glitch
    -- propagates straight to ack, creating a handshake race.
    ack <= req;
end architecture behavioral;
