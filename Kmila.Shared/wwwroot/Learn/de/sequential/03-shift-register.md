# Beispiel: 8-Bit-Schieberegister

Ein Schieberegister ist eine Reihe von Flip-Flops, die zusammengekettet sind. Jede Taktflanke verschiebt die gespeicherten Bits um eine Position, und an einem Ende kommt ein neues Bit herein. Sie stecken im Herzen von UARTs, SPI-Controllern, CRC-Engines und LFSR-basierten Zufallszahlengeneratoren — der praktische Lohn der Flip-Flops aus der vorherigen Lektion.

## Was dieses Beispiel macht

`shift8` ist ein **Serial-In, Parallel-Out** (SIPO) Schieberegister:

- `din` ist das neue Bit, das bei jeder steigenden Taktflanke ankommt.
- `r` ist das interne 8-Bit-Register.
- `q` zeigt `r` nach außen als paralleles Byte.

„Serial in“, weil die Bits eins nach dem anderen hereinkommen; „parallel out“, weil du alle acht auf einmal lesen kannst. Genau das tut ein UART-Empfänger: Er taktet die Bits eins nach dem anderen von der Leitung herein und übergibt dann das zusammengesetzte Byte dem Rest des Systems.

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

`&` ist der **Konkatenationsoperator**. `r(6 downto 0)` sind die unteren 7 Bits des aktuellen Registers. Wir kleben `din` ans rechte Ende, machen einen neuen 8-Bit-Wert daraus und weisen ihn `r` zurück. Weil das eine Signalzuweisung in einem getakteten Prozess ist, passiert die Verschiebung bei jeder steigenden Flanke — und weil Signale am Ende des Schritts aktualisieren, nutzt die rechte Seite das *alte* `r`, was genau das ist, was eine Verschiebung braucht.

## Es sind nur acht Flip-Flops in einer Kette

Lass den einzeiligen Body nicht die Hardware verstecken. `r` sind acht D-Flip-Flops. Die Zuweisung verdrahtet den Ausgang jedes Flip-Flops mit dem Eingang des nächsten, wobei `din` das erste speist und Bit 7 am Ende herausfällt (hier verworfen). Diese Kette *ist* das Schieberegister — die Konkatenation ist nur eine kompakte Art, die Verdrahtung zu beschreiben.

## Wellenform-Durchgang

Die Sim-Config treibt `din` durch ein Muster: `1, 0, 1, 1, 0, 1, 0, 1`. Beobachte `q` über die Zeit:

- Nach dem ersten Tick: `q = 00000001`
- Nach dem zweiten: `q = 00000010`
- Nach dem dritten: `q = 00000101`
- …und so weiter. Das neue Bit landet im LSB; ältere Bits marschieren Richtung MSB.

Wenn du rauszoomst, siehst du den ersten `din`-Wert das oberste Bit (Bit 7) genau 8 Takte nach seiner Ankunft erreichen. Das ist die Round-Trip-Latenz eines 8-Bit-Schieberegisters — und der Grund, warum eine serielle Verbindung eine feste, vorhersagbare Verzögerung hat.

## Eine Falle: in die falsche Richtung schieben

Anfänger drehen oft die Richtung um und verlieren ein Bit. Wenn du `r <= r(7 downto 1) & din;` schreibst, hast du die *oberen* sieben Bits behalten und Bit 0 verworfen — das ist eine Verschiebung *nach rechts*, nicht nach links. Entscheide, an welchem Ende neue Daten hereinkommen und welches herausfällt, und lass dann den Slice dazu passen. Die zwei korrekten Formen sind:

```vhdl
r <= r(6 downto 0) & din;   -- Schiebe LINKS: din -> LSB, MSB verworfen
r <= din & r(7 downto 1);   -- Schiebe RECHTS: din -> MSB, LSB verworfen
```

## Du bist dran

- **Stattdessen nach rechts schieben.** Nutze die zweite Form oben und bestätige, dass das wandernde Bit sich jetzt in die andere Richtung bewegt.
- **Parallel laden (PISO/universal).** Füge einen `load`-Eingang und einen `din_par`-Bus hinzu. Wenn `load = '1'` an einer Taktflanke ist, `r <= din_par` (nimm den ganzen Bus auf einmal); sonst schieben. Das ist das Herz eines *Parallel-In, Serial-Out*-Senders.
- **Serieller Ausgang.** Füge `dout <= r(7);` hinzu. Jetzt ist es Serial-In, Serial-Out (SISO) — das erste Bit, das du auf `din` gesendet hast, erscheint acht Zyklen später auf `dout`, die Verzögerung, auf die sich ein UART-RX verlässt.
- **Mach ein LFSR.** Füttere `r(7) xor r(5) xor r(4) xor r(3)` statt `din` zurück in den Eingang. Du hast jetzt einen Pseudozufalls-Sequenzgenerator — dieselbe Struktur, die für CRCs und Testmuster genutzt wird.

## Was du behalten solltest

- Ein Schieberegister ist eine Kette von D-Flip-Flops; jede Flanke bewegt jedes Bit um eine Stelle und lässt an einem Ende ein neues Bit herein.
- `r <= r(6 downto 0) & din;` schiebt nach links (neues Bit ins LSB); tausche Slice und Reihenfolge, um nach rechts zu schieben.
- Ein paralleles Laden hinzuzufügen macht daraus einen SIPO/PISO-Wandler — das Herz serieller Verbindungen wie UART und SPI.
- Füttere ausgewählte Taps durch XOR zurück und es wird zu einem LFSR für CRCs und Pseudozufalls-Sequenzen.
