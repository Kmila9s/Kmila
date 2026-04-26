# Beispiel: 8-Bit-Schieberegister

Ein Schieberegister ist eine Reihe verketteter Flipflops. Jede Taktflanke schiebt die gespeicherten Bits eine Position weiter, und ein neues Bit tritt an einem Ende ein. Sie stecken im Kern von UARTs, SPI-Controllern, CRC-Engines und LFSR-basierten Zufallszahlengeneratoren.

## Was dieses Beispiel macht

`shift8` ist ein **Serial-In-Parallel-Out**-Schieberegister:

- `din` ist das neue Bit, das bei jeder steigenden Taktflanke eintrifft.
- `r` ist das interne 8-Bit-Register.
- `q` stellt `r` der Außenwelt als paralleles Byte zur Verfügung.

## Der Rumpf

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

`&` ist der **Konkatenations**-Operator. `r(6 downto 0)` sind die unteren 7 Bits des aktuellen Registers. Wir hängen `din` ans rechte Ende an, bilden einen neuen 8-Bit-Wert und weisen ihn zurück nach `r` zu.

Weil das eine Signalzuweisung in einem getakteten Prozess ist, passiert das Schieben an jeder steigenden Flanke.

## Waveform-Durchlauf

Die Sim-Konfiguration treibt `din` durch ein Muster: `1, 0, 1, 1, 0, 1, 0, 1`. Beobachte `q` über die Zeit:

- Nach dem ersten Tick: `q = 00000001`
- Nach dem zweiten: `q = 00000010`
- Nach dem dritten: `q = 00000101`
- …und so weiter. Das neue Bit landet im LSB; ältere Bits wandern Richtung MSB.

Wenn du herauszoomst, siehst du, wie das oberste Bit (Bit 7) den ersten `din`-Wert genau 8 Taktzyklen nach seiner Ankunft bekommt. Das ist die Rundtrip-Latenz eines 8-Bit-Schieberegisters.

## Du bist dran

- **Schiebe stattdessen nach rechts.** Tausche die Konkatenation: `r <= din & r(7 downto 1);`.
- **Parallele Ladung.** Füge einen `load`-Eingang und einen `din_parallel`-Bus hinzu. Wenn `load = '1'` an einer Taktflanke, übernimmt das Register den gesamten Bus auf einmal, statt zu schieben.
- **Serieller Ausgang.** Füge `dout <= r(7);` hinzu. Jetzt ist es ein Serial-In-Serial-Out- (SISO-) Schieberegister — das erste Bit, das du auf `din` gesendet hast, erscheint acht Zyklen später auf `dout`. So funktioniert die Bit-für-Bit-Verzögerung im UART-RX.
