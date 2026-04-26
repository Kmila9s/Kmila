# Die Testbench-Denkweise

In jeder Lektion bis hierher war Kmila dein Testbench — du hast Stimuli in den Ports-Tab gezogen, Run hat das Design laufen lassen, und du hast die Waveform begutachtet. Das ist fürs Lernen in Ordnung. Es ist nicht, wie professionelles Digitaldesign funktioniert.

## Was ein echtes Testbench ist

Ein **Testbench** ist eine weitere VHDL-Entity, deren einzige Aufgabe es ist, ein *Design under Test* (DUT) zu stimulieren. Es hat keine Ports, weil nichts es treibt; es deklariert das DUT als Komponente, verdrahtet seine eigenen internen Signale mit den Ports des DUT und verwendet dann einen **Prozess**, um Stimulussequenzen über die Zeit zu durchlaufen.

Skelett:

```vhdl
entity tb_counter is
end entity tb_counter;

architecture sim of tb_counter is
    component counter
        port ( clk, reset : in  std_logic;
               q          : out std_logic_vector(3 downto 0) );
    end component;

    signal clk   : std_logic := '0';
    signal reset : std_logic := '1';
    signal q     : std_logic_vector(3 downto 0);
begin
    dut : counter port map (clk, reset, q);

    clk <= not clk after 10 ns;            -- 50 MHz

    process
    begin
        wait for 20 ns;
        reset <= '0';
        wait for 200 ns;
        assert q = "1010"
            report "Counter should be 10 at 200 ns, got " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

Drei neue Ideen:

1. **`wait`** — Zeit vergeht nur dann, wenn ein Prozess es sagt.
2. **`assert … report … severity`** — die Selbstprüfung. Macht aus einer 1000-zeiligen Waveform ein "Pass" oder "Fail".
3. **Port-lose Entity** — Testbenches sind eigenständig; der Simulator instanziiert sie direkt.

## Warum das wichtig ist

- **Regression**: Ein Testbench, das du gestern geschrieben hast, läuft heute immer noch. Eine Waveform, die du gestern angestarrt hast, ist weg.
- **Abdeckung**: Du kannst Stimulusmuster ansteuern, die eine interaktive Sitzung nie erreicht (zufällige Eingaben, Randfälle, Eckfrequenzen).
- **Automation**: Ein CI-System führt dein Testbench bei jedem Commit aus. Waveforms passen nicht in CI.

## Kmilas aktuelle Grenzen

Der Kmila-Simulator führt Testbenches problemlos aus, wenn du sie als reguläre Entities schreibst — das einzige, was heute fehlt, ist die automatische Pass/Fail-Extraktion aus `assert`-Anweisungen in den Output-Log. Das ist auf der Roadmap.

*Weitere Lektionen folgen — dieses Modul wird aktiv geschrieben.*
