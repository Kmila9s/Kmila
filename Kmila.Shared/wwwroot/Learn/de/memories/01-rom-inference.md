# Beispiel: abgeleiteter synchroner ROM

Jeder FPGA hat **Block-RAM** (BRAM) — dedizierte Speicherblöcke, die der Synthesizer verwendet, wenn er ein ROM- oder RAM-Muster in deinem Code erkennt. Du instanziierst keine Vendor-Primitive; du schreibst einfaches VHDL in einer Form, die das Tool mappen kann.

Dieses Beispiel ist das portable Lehrbuch-ROM-Muster. Jeder ernsthafte Synthesizer (Xilinx Vivado, Intel Quartus, Lattice Radiant, Yosys) erkennt es und ordnet automatisch BRAM zu.

## Das Muster

```vhdl
type rom_t is array(0 to 15) of std_logic_vector(7 downto 0);
constant ROM : rom_t := (
  x"00", x"01", x"04", x"09", x"10", x"19", x"24", x"31",
  x"40", x"51", x"64", x"79", x"90", x"A9", x"C4", x"E1"
);

process(clk)
begin
  if rising_edge(clk) then
    data <= ROM(to_integer(unsigned(addr)));
  end if;
end process;
```

Drei Dinge machen das zu einem BRAM-freundlichen ROM:

1. **Der Speicher ist eine Konstante** (`constant ROM : rom_t := (...)`). Die Daten stehen zur Synthesezeit fest.
2. **Der Read ist getaktet.** Ein kombinatorischer ROM-Read kann nur auf LUTs gemappt werden — bei einem winzigen 16-Eintrag-ROM okay, aber alles Größere braucht das getaktete Muster, damit der Synthesizer BRAM allokiert.
3. **`addr` wird rein als Index benutzt.** Keine teure kombinatorische Logik zwischen `addr` und dem Speicher-Lookup.

## Warum dieser Inhalt?

Der Beispiel-ROM hält `n²` für `n = 0..15`. Nützlich als Lookup-Quadrier-Einheit — schneller als ein vollständiger Multiplizierer, wenn du nur kleine Operanden brauchst. Du könntest auch speichern:

- Eine Sinus-Tabelle (für DDS-Signalgeneratoren)
- CRC-Polynome
- Protokoll-Lookup-Tabellen
- Zeichensätze

## Was das Beispiel tut

Die Sim treibt `addr` durch 5 spezifische Werte: 0, 1, 4, 8, 15. Nach der Taktflanke zeigt `data` den entsprechenden ROM-Eintrag, einen Zyklus später (das ist die Read-Latenz eines synchronen ROMs — du fragst nach der Adresse, du bekommst die Daten beim **nächsten** Takt).

## Du bist dran

- **Mach es parametrisierbar.** Ersetze die feste 16-Eintrag-Größe durch `generic (DEPTH : integer := 16; WIDTH : integer := 8)`. Der Rest soll weiterhin funktionieren.
- **Lies den Inhalt aus einer Datei.** Echte Projekte laden ROM-Inhalte über `init_from_file()` oder ein separates Konstanten-Package. Versuche, VHDL zur Elaborationszeit eine Textdatei mit Hex-Werten lesen zu lassen (`textio` + `hread`).
- **Füge einen zweiten Read-Port hinzu.** Für Dinge wie Instruction-/Data-Memory in einer Single-Cycle-CPU. Du siehst dieses Muster später im Curriculum im CPU-Projekt.
