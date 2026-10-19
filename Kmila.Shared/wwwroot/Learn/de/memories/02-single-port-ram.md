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

Warum ein **Signal** und nicht eine `variable` oder Konstante? Das Array muss gemeinsamer Zustand sein, der über Taktflanken hinweg bestehen bleibt und zur Laufzeit *neu geschrieben* wird – genau das ist ein Signal. Eine Konstante (wie im ROM-Beispiel) kann nicht geschrieben werden; eine Prozess-Variable wäre außerhalb des Prozesses nicht sichtbar. Und weil jeder Zugriff innerhalb von `rising_edge(clk)` liegt, sind sowohl der Schreib- als auch der Lesevorgang **synchron** – und das ist es, was das Tool das Ganze auf ein Block-RAM mappen lässt.

## Die drei Lese-Politiken

Jedes synchrone RAM muss entscheiden, was `dout` **während eines Schreibvorgangs** auf die gerade gelesene Adresse tut. Dieses „Read-during-Write"-Verhalten hat drei Standard-Konventionen, und die Form deines Prozesses wählt, welche du bekommst:

- **Write-first** (auch Read-through genannt, oben gezeigt): Beim Schreiben zeigt `dout` die neu geschriebenen Daten. Sauberste Semantik – ein Read und ein Write auf dieselbe Adresse im selben Zyklus liefern dir den frischen Wert.
- **Read-first:** Beim Schreiben zeigt `dout` den **alten** Inhalt der Zelle. Manche CPU-Registersätze nutzen das.
- **No-change:** Beim Schreiben aktualisiert `dout` gar nicht – es hält seinen letzten Wert. Verbreitet in Low-Power-Designs, da das Ausgangsregister beim Schreiben nicht umgeschaltet wird.

Der Unterschied ist rein die *Reihenfolge* der beiden Zuweisungen im Prozess:

```vhdl
-- READ-FIRST: alte Daten erfassen, dann überschreiben
if we = '1' then
  dout <= mem(to_integer(unsigned(addr)));   -- alter Wert raus
  mem(to_integer(unsigned(addr))) <= din;    -- dann schreiben
end if;

-- NO-CHANGE: dout nur aktualisieren, wenn NICHT geschrieben wird
if we = '1' then
  mem(to_integer(unsigned(addr))) <= din;    -- dout bleibt unangetastet
else
  dout <= mem(to_integer(unsigned(addr)));
end if;
```

Alle drei mappen auf jedem modernen FPGA auf Block-RAM, und jeder Port eines echten Dual-Port-RAMs kann sogar einen anderen Modus nutzen. Aber der Synthesizer wählt den Modus aus der *exakten* Form deines Prozesses, also passe die Vorlage an das Verhalten an, das du wirklich willst. Die Write-First-Form ist die am breitesten portable – im Zweifel schreib sie.

## Was das Beispiel tut

In den ersten 30 µs schreibt die Sim auf drei Adressen (`0`, `1`, `2`) mit drei verschiedenen Byte-Mustern. Dann fällt `we`, und `addr` wird bei 40 µs zurück auf 0 gesetzt. Der Wellenform-Tab zeigt `dout`, das während des Schreibens `din` folgt, und dann beim Lesen den gespeicherten Wert wieder ausgibt – einen Zyklus nachdem du die Adresse anlegst, weil der Read registriert ist.

## Darauf musst du achten

- **Adressbreite vs. Tiefe.** Genau wie beim ROM muss `to_integer(unsigned(addr))` innerhalb von `0 .. 31` bleiben. Dimensioniere `addr` auf `log2(DEPTH)` Bit, damit es nicht über-indizieren kann.
- **Nicht kombinatorisch lesen.** Ein kombinatorischer Read (`dout <= mem(...)` außerhalb des getakteten Prozesses) blockiert meist die BRAM-Ableitung und zwingt das Array in Flip-Flops/LUTs – okay für ein Dutzend Bytes, katastrophal für Kilobytes.
- **Initialisierer sind nicht garantiert.** Der Initialwert `signal mem := (others => ...)` funktioniert in der Simulation und auf vielen FPGAs (wo BRAM einen Einschaltzustand hat), aber nicht auf allen Targets und nie bei echtem externem SRAM/DRAM, das undefiniert hochfährt. Verlass dich in Produktion nicht ohne Prüfung deines Devices darauf.

## Du bist dran

- **Upgrade zu echtem Dual-Port-RAM.** Zwei volle Sätze `addr`/`din`/`dout`/`we`, beide auf `clk` getaktet. Das ist das Muster, das alle FIFOs und Dual-Clock-DPRAMs der Hersteller darunter verwenden.
- **Füge ein Ausgangsregister hinzu.** Block-RAM unterstützt ein zweites Pipeline-Register nach dem Speicher-Array. Ergänze `dout_reg <= dout;` in einem zweiten getakteten Block und beobachte, was sich auf der Wellenform ändert (Hinweis: ein Zyklus mehr Latenz, aber viel besseres Timing-Closure).
- **Bau ein FIFO darauf.** Umhülle das RAM mit einem Schreibzeiger, einem Lesezeiger und `full`/`empty`-Flags. So baut man ein synchrones FIFO standardmäßig, und es wird in dieser Einheit namentlich verlangt.

## Bezug zum Kurs

Das ist der Kern von Einheit IV, *Memorias*: RAM / SRAM / DRAM und der Entwurf von RAM-, ROM- und **FIFO**-Speichern in HDL per Array-Handling. Write-first/Read-first/No-change ist das HDL-Gesicht des Read-during-Write-Verhaltens, das echte SRAM-Zellen zeigen; die FIFO-Übung oben ist genau die *tipos especiales de memorias*, die die Einheit auflistet.

## Was du behalten solltest

- Lese-Schreib-Speicher = dasselbe Array-Muster wie beim ROM, aber ein **Signal**, das du zur Laufzeit beschreibst.
- Halte sowohl Read als auch Write innerhalb von `rising_edge(clk)`, damit das Tool Block-RAM ableitet.
- Write-first / Read-first / No-change werden durch die Reihenfolge der Anweisungen im Prozess gewählt – nimm die, die dein Design braucht; Write-first ist der sichere Standard.
- Achte auf die Adressbreite, vermeide kombinatorische Reads und vertrau BRAM-Initialisierern nicht auf allen Targets.
