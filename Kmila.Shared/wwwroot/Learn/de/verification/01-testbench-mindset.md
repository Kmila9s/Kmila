# Die Testbench-Denkweise

In allen bisherigen Lektionen war Kmila deine Testbench – du hast Stimuli in den Ports-Tab gezogen, Run hat das Design ausgeführt, und du hast die Wellenform mit dem Auge geprüft. Zum Lernen ist das okay. So funktioniert professionelles Digitaldesign nicht.

## Was eine echte Testbench ist

Eine **Testbench** ist eine weitere VHDL-Entity, deren einzige Aufgabe es ist, ein *Design under Test* (DUT) zu fordern. Sie hat keine Ports, weil sie von nichts getrieben wird; sie deklariert das DUT als Komponente, verbindet ihre eigenen internen Signale mit den Ports des DUT und nutzt dann einen **Prozess**, um zeitlich Stimulus-Sequenzen abzulaufen.

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

1. **`wait`** – Zeit vergeht nur, wenn ein Prozess es sagt.
2. **`assert … report … severity`** – die Selbstprüfung. Macht aus einer 1000-Zeilen-Wellenform ein „pass" oder „fail".
3. **Entity ohne Ports** – Testbenches sind in sich abgeschlossen; der Simulator instanziiert sie direkt.

## Einen Takt erzeugen

Dieser Einzeiler, `clk <= not clk after 10 ns;`, ist der ganze Takt: Er kippt alle 10 ns und ergibt eine Periode von 20 ns (50 MHz). Weil `clk` einen Anfangswert von `'0'` hat, startet er sauber. Für einen Takt mit anderem Tastverhältnis oder einen, den du später anhalten musst, nutze stattdessen einen Prozess:

```vhdl
clk_gen : process
begin
    clk <= '0'; wait for 10 ns;
    clk <= '1'; wait for 10 ns;
    if done then wait; end if;   -- einfrieren, wenn der Test vorbei ist
end process;
```

Halte Takterzeugung und Stimulus in **getrennten** Prozessen. Der Takt tickt einfach; der Stimulus-Prozess treibt Eingänge und prüft Ausgänge, meist synchronisiert mit `wait until rising_edge(clk)`, damit er sich am DUT ausrichtet.

## Selbstprüfung: assert, severity und Golden Values

Der Sinn einer Testbench ist, dass sie sich *selbst* prüft. `assert` wertet eine Bedingung aus und gibt, falls sie falsch ist, den `report`-String mit der gewählten `severity` aus. Die vier Severity-Stufen sind **note**, **warning**, **error** und **failure** – und den meisten Simulatoren kann man sagen, dass sie in dem Moment anhalten sollen, in dem eine einer bestimmten Stufe feuert. So machst du aus „sah richtig aus" ein hartes Pass/Fail.

Für nichttriviale Designs schreib nicht jeden erwarteten Wert von Hand. Berechne ihn aus einem **Golden Model** – einer einfachen, offensichtlich korrekten Referenz (auch eine schlichte software-artige Berechnung in der Testbench) – und vergleiche das DUT in jedem Zyklus damit. Dieser Vergleich-gegen-eine-Referenz ist die Kernidee hinter einem **Scoreboard**, und sie skaliert von einem Zähler bis zu einer CPU.

```vhdl
expected <= std_logic_vector(unsigned(expected) + 1) when rising_edge(clk);
assert q = expected
    report "Abweichung: erhalten " & to_string(q) & " erwartet " & to_string(expected)
    severity error;
```

(`to_string` für `std_logic_vector` ist eine Bequemlichkeit aus VHDL-2008 – kompiliere die Testbench im 2008-Modus, um es zu nutzen.)

## Die Simulation sauber beenden

Eine reine Wellenform-Sitzung läuft einfach ewig. Eine selbstprüfende Testbench sollte sich **selbst anhalten**, wenn der Stimulus erschöpft ist. In VHDL-2008 ist der saubere Weg `finish` aus dem Package `std.env`:

```vhdl
use std.env.all;
...
report "Alle Tests bestanden";
finish;          -- beendet die Simulation; stop würde zur Inspektion pausieren
```

`finish` beendet den Lauf (ideal für automatisierten/CI-Einsatz); `stop` pausiert ihn, damit du interaktiv herumstöbern kannst. Der ältere Trick, ein `assert … severity failure` zum Anhalten zu erzwingen, funktioniert auch in jeder VHDL-Version, meldet aber selbst bei einem bestandenen Lauf einen *failure*, was Regressions-Tools verwirrt – bevorzuge `finish`.

## Gerichteter vs. randomisierter Stimulus

- **Gerichtete** Tests treffen spezifische, handgewählte Fälle: Reset-Verhalten, den Überlauf eines Zählers, eine bekannte heikle Sequenz. Super, um einen Bug festzunageln, und für die Akzeptanzfälle in einer Spec.
- **Randomisierte / constrained-random** Tests werfen viele legale-aber-ungeplante Eingaben auf das DUT und fangen Ecken, die du nie von Hand schreiben würdest. Sie lehnen sich am Golden Model an, weil du nicht jedes Ergebnis von Hand vorhersagen kannst.

Die meiste reale Verifikation mischt beides: gerichtete Tests für die Fälle, von denen du weißt, dass sie zählen, randomisierte für die, von denen du es nicht weißt.

## Warum das wichtig ist

- **Regression:** Eine Testbench, die du gestern geschrieben hast, läuft heute noch. Eine Wellenform, auf die du gestern gestarrt hast, ist weg.
- **Coverage:** Du kannst Stimulus-Muster fahren, die eine interaktive Sitzung nie erreicht (Zufallseingaben, Randfälle, Eckfrequenzen).
- **Automatisierung:** Ein CI-System fährt deine Testbench bei jedem Commit und liest Pass/Fail aus dem Exit-Code. Wellenformen passen nicht in CI.

## Bezug zum Kurs

Verifikation ist das stille Rückgrat der gesamten Laborsequenz von *Diseño de Sistemas Digitales*. Jede Praktik – Register, sequentielle Systeme, Zähler, Speicher, ASM-Maschinen – ist eigentlich „bau es in HDL, dann beweise, dass es funktioniert". Eine selbstprüfende Testbench ist der Weg, jeden *reporte de práctica* von „hier ein Screenshot einer Wellenform" zu „hier ein Test, der besteht" zu machen – genau die Strenge, die die problembasierte Bewertung der Einheit anstrebt.

## Kmilas aktuelle Grenzen

Der Kmila-Simulator führt Testbenches problemlos aus, wenn du sie als gewöhnliche Entities schreibst – heute fehlt nur die automatische Pass/Fail-Extraktion aus `assert`-Anweisungen in den Output-Log. Das steht auf der Roadmap.

## Was du behalten solltest

- Eine Testbench ist eine portlose Entity, die das DUT instanziiert, einen Takt erzeugt, Stimulus treibt und sich mit `assert` **selbst prüft**.
- Halte Takt und Stimulus in getrennten Prozessen; synchronisiere Prüfungen mit `wait until rising_edge(clk)`.
- Vergleiche gegen ein Golden Model (ein Scoreboard), statt jeden erwarteten Wert von Hand zu schreiben.
- Beende automatisierte Läufe mit `std.env.finish`; nutze Severity-Stufen, damit der Simulator bei echten Fehlern anhalten kann.
- Mische gerichtete Tests (bekannte Fälle) mit randomisierten (den Ecken, die du nie erraten würdest).
