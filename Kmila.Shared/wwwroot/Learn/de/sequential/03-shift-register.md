# Beispiel: 8-Bit-Schieberegister

Ein Schieberegister ist eine Reihe von Flip-Flops, die zusammengekettet sind. Jede Taktflanke verschiebt die gespeicherten Bits um eine Position, und an einem Ende kommt ein neues Bit rein. Sie stecken im Herzen von UARTs, SPI-Controllern, CRC-Engines und LFSR-basierten Zufallszahlengeneratoren.

## Was dieses Beispiel macht

`shift8` ist ein **Serial-In, Parallel-Out**-Schieberegister:

- `din` ist das neue Bit, das bei jeder steigenden Taktflanke ankommt.
- `r` ist das interne 8-Bit-Register.
- `q` zeigt `r` nach außen als paralleles Byte.

## Der Body

```vhdl
signal r : std_logic_vector(7 downto 0) := (others => '0');
...
process(clk, reset)
begin
  if reset = '1' then
    r <= (others => '0');
  elsif rising_edge(clk) then
    r <= r(6 downto 0) & din;
  end if;
end process;

q <= r;
```

Die Magie liegt in dieser Zeile:

```vhdl
r <= r(6 downto 0) & din;
```

`&` ist der **Konkatenationsoperator**. `r(6 downto 0)` sind die unteren 7 Bits des aktuellen Registers. Wir kleben `din` ans rechte Ende, machen einen neuen 8-Bit-Wert daraus und weisen ihn `r` zu.

Weil das eine Signalzuweisung in einem getakteten Prozess ist, passiert die Verschiebung bei jeder steigenden Flanke.

## Wellenform-Durchgang

Die Sim-Config treibt `din` durch ein Muster: `1, 0, 1, 1, 0, 1, 0, 1`. Beobachte `q` über die Zeit:

- Nach dem ersten Tick: `q = 00000001`
- Nach dem zweiten: `q = 00000010`
- Nach dem dritten: `q = 00000101`
- …und so weiter. Das neue Bit landet im LSB; ältere Bits wandern Richtung MSB.

Wenn du rauszoomst, siehst du das oberste Bit (Bit 7) den ersten `din`-Wert genau 8 Takte später erreichen. Das ist die Round-Trip-Latenz eines 8-Bit-Schieberegisters.

## Du bist dran

- **Stattdessen nach rechts schieben.** Tausche die Konkatenation: `r <= din & r(7 downto 1);`.
- **Parallel laden.** Füge einen `load`-Eingang und einen `din_parallel`-Bus hinzu. Wenn `load = '1'` an einer Taktflanke ist, übernimmt das Register den ganzen Bus auf einmal statt zu schieben.
- **Serieller Ausgang.** Füge `dout <= r(7);` hinzu. Jetzt ist es ein Serial-In-Serial-Out-(SISO)-Schieberegister — das erste auf `din` gesendete Bit erscheint 8 Zyklen später auf `dout`. So funktioniert die Bit-für-Bit-Verzögerung im UART-RX.
