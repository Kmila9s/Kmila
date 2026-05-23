# Beispiel: Single-Port-RAM mit Write-First-Read

Lese-Schreib-Speicher ist die andere Hälfte des „BRAM-Ableitens". Dasselbe `type…array…of`-Muster funktioniert, nur ist das Array jetzt ein **Signal**, und du schreibst auf einigen Taktflanken und liest auf den anderen.

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

Jeder synchrone RAM muss entscheiden, was `dout` **während eines Schreibvorgangs** tut. Drei Konventionen:

- **Write-first (oben gezeigt):** beim Schreiben zeigt `dout` die neu geschriebenen Daten. Sauberste Semantik — ein Read und ein Write auf dieselbe Adresse im selben Zyklus liefern dir den frischen Wert.
- **Read-first:** beim Schreiben zeigt `dout` den **alten** Inhalt der Zelle. Manche CPU-Registerfiles nutzen das.
- **No-change:** beim Schreiben aktualisiert `dout` nicht. Manche Low-Power-Designs.

Alle drei mappen auf Block-RAM auf jedem modernen FPGA, aber der Synthesizer wählt den Modus basierend auf der genauen Form deines Prozesses. Die Write-First-Vorlage ist die portabelste — im Zweifel die nehmen.

## Was das Beispiel tut

In den ersten 30 µs schreibt die Sim auf drei Adressen (`0`, `1`, `2`) mit drei verschiedenen Byte-Mustern. Dann fällt `we`, und `addr` wird bei 40 µs zurück auf 0 gesetzt. Der Wellenform-Tab zeigt `dout`, das während des Schreibens `din` folgt, und dann beim Lesen den gespeicherten Wert wieder ausgibt.

## Du bist dran

- **Upgrade zu echtem Dual-Port-RAM.** Zwei volle Sätze `addr`/`din`/`dout`/`we`, beide auf `clk` getaktet. Das ist das Muster, das alle FIFOs und Dual-Clock-DPRAMs der Hersteller darunter verwenden.
- **Füge ein Output-Register hinzu.** Block-RAM auf Xilinx unterstützt ein zweites Pipeline-Register nach dem Speicher-Array. Ergänze `dout_reg <= dout;` in einem zweiten getakteten Block und beobachte den Unterschied auf der Wellenform (Hinweis: ein Zyklus mehr Latenz, aber viel besseres Timing-Closure).
- **Ändere den Anfangsinhalt.** Belege die ersten Einträge mit einem erkennbaren Muster, indem du den Initialwert von `signal mem` änderst. Nicht alle Synthesizer respektieren BRAM-Initialisierer in portablem VHDL — prüfe dein Target, bevor du dich in Produktion darauf verlässt.
