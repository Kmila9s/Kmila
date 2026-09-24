# Die Testbench-Denkweise

In allen bisherigen Lektionen war Kmila deine Testbench — du hast Stimuli in den Ports-Tab gezogen, Run hat das Design ausgeführt, und du hast die Wellenform mit dem Auge geprüft. Zum Lernen ist das okay. So funktioniert professionelles Digitaldesign nicht.

## Was eine echte Testbench ist

Eine **Testbench** ist eine weitere VHDL-Entity, deren einzige Aufgabe es ist, ein *Design under Test* (DUT) zu fordern. Sie hat keine Ports, weil sie von niemandem getrieben wird; sie deklariert das DUT als Komponente, verbindet ihre eigenen internen Signale mit den Ports des DUT und nutzt dann einen **Prozess**, um zeitlich Stimulus-Sequenzen abzulaufen.

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
            report "Counter sollte bei 200 ns 10 sein, erhalten " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

Drei neue Ideen:

1. **`wait`** — Zeit vergeht nur, wenn ein Prozess es sagt.
2. **`assert … report … severity`** — die Selbstprüfung. Macht aus einer 1000-Zeilen-Wellenform ein „pass" oder „fail".
3. **Entity ohne Ports** — Testbenches sind in sich abgeschlossen; der Simulator instanziiert sie direkt.

## Warum das wichtig ist

- **Regression**: eine Testbench, die du gestern geschrieben hast, läuft heute noch. Eine Wellenform, auf die du gestern gestarrt hast, ist weg.
- **Coverage**: du kannst Stimulus-Muster fahren, die eine interaktive Sitzung nie erreicht (Zufallseingaben, Randfälle, Eckfrequenzen).
- **Automatisierung**: ein CI-System fährt deine Testbench bei jedem Commit. Wellenformen passen nicht in CI.

## Kmilas aktuelle Grenzen

Der Kmila-Simulator führt Testbenches problemlos aus, wenn du sie als gewöhnliche Entities schreibst — heute fehlt nur die automatische Pass/Fail-Extraktion aus `assert`-Anweisungen in den Output-Log. Steht auf der Roadmap.

*Weitere Lektionen folgen — dieses Modul ist in aktiver Bearbeitung.*
