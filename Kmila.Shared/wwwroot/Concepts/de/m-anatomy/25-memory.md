# ROM-Speicher

**Speicher** hält Wörter, die man per **Adresse** zurücklesen kann. Ein ROM hat festen Inhalt; jede Adresse wählt ein Wort, das am Ausgang erscheint.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/25-memory.html" title="Interaktive Abbildung" loading="lazy"></iframe></div>

Die Adress-Taster wählen eine Zelle; ihr gespeichertes Wort lässt die Ausgangslampen leuchten, und die Tabelle zeigt jede Adresse → Daten.

In VHDL:

```vhdl
type rom_t is array(0 to 3) of unsigned(2 downto 0);
constant ROM : rom_t := ("101","011","110","001");
...
data <= ROM(to_integer(addr));  -- addressed read
```

Lesen ist reines Adressieren — ein ROM ist buchstäblich ein N-zu-1-MUX je Ausgangsbit. RAM fügt einen Schreibpfad hinzu; Block-RAM (BRAM) ist dedizierter Speicher im FPGA.
