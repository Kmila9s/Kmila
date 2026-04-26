# Beispiel: Single-Port-RAM mit Write-First-Read

Lese-Schreib-Speicher ist die andere Hälfte der "BRAM-Inferenz". Dasselbe `type…array…of`-Muster funktioniert, aber jetzt ist das Array ein **Signal**, und du schreibst an einigen Taktflanken und liest an anderen.

## Die Vorlage

```vhdl
type mem_t is array(0 to 31) of std_logic_vector(7 downto 0);
signal mem : mem_t := (others => (others => '0'));
...
process(clk)
begin
  if rising_edge(clk) then
    if we = '1' then
      mem(to_integer(unsigned(addr))) <= din;
      dout <= din;                             -- write-first read
    else
      dout <= mem(to_integer(unsigned(addr)));
    end if;
  end if;
end process;
```

## Die drei Lesepolitiken

Jedes synchrone RAM muss entscheiden, was `dout` **während eines Schreibzugriffs** tut. Drei Konventionen existieren:

- **Write-first (oben gezeigt):** Bei einem Schreibzugriff zeigt `dout` die neu geschriebenen Daten. Sauberste Semantik — ein Lese- und Schreibzugriff auf dieselbe Adresse im selben Zyklus liefert dir den frischen Wert.
- **Read-first:** Bei einem Schreibzugriff zeigt `dout` den **alten** Inhalt der Zelle. Die Registerdateien mancher CPUs verwenden das.
- **No-change:** Bei einem Schreibzugriff wird `dout` nicht aktualisiert. Manche Low-Power-Designs.

Alle drei werden auf jedem modernen FPGA auf Block-RAM abgebildet, aber der Synthesizer wählt den Modus basierend auf der genauen Form deines Prozesses. Die Write-first-Vorlage ist die portabelste — im Zweifelsfall schreibe sie.

## Was das Beispiel macht

Während der ersten 30 µs schreibt die Simulation an drei Adressen (`0`, `1`, `2`) mit drei verschiedenen Bytemustern. Dann fällt `we` ab, und `addr` wird bei 40 µs auf 0 zurückgesetzt. Der Waveforms-Tab zeigt `dout`, das `din` während der Schreibzugriffe folgt, und dann den gespeicherten Wert zurückgibt, wenn du liest.

## Du bist dran

- **Rüste auf ein echtes Dual-Port-RAM auf.** Zwei vollständige Sätze von `addr`/`din`/`dout`/`we`, beide mit `clk` getaktet. Das ist das Muster, das jedem FIFOs und Dual-Clock-DPRAMs jedes Herstellers zugrunde liegt.
- **Füge ein Ausgangsregister hinzu.** Block-RAM auf Xilinx unterstützt ein zweites Pipeline-Register nach dem Speicherarray. Füge `dout_reg <= dout;` in einem zweiten getakteten Block hinzu und beobachte, was sich in der Waveform ändert (Hinweis: ein Zyklus mehr Latenz, aber viel bessere Timing-Closure).
- **Ändere den Anfangsinhalt.** Befülle die ersten Einträge mit einem erkennbaren Muster, indem du den Initialisierer von `signal mem` änderst. Nicht alle Synthesizer respektieren BRAM-Initialisierer in portablem VHDL — prüfe dein Ziel, bevor du dich in der Produktion darauf verlässt.
