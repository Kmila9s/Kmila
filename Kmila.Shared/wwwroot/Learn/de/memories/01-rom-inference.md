# Beispiel: inferiertes synchrones ROM

Jeder FPGA hat **Block-RAM** (BRAM) — dedizierte Speicherblöcke, die der Synthesizer verwendet, wenn er ein ROM- oder RAM-Muster in deinem Code erkennt. Du instanziierst keine Hersteller-Primitiven; du schreibst schlichtes VHDL in einer Form, die das Tool zuordnen kann.

Dieses Beispiel ist das portable, lehrbuchmäßige ROM-Muster. Jeder ernsthafte Synthesizer (Xilinx Vivado, Intel Quartus, Lattice Radiant, Yosys) erkennt es und alloziert automatisch BRAM.

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

Drei Dinge machen dies zu einem BRAM-freundlichen ROM:

1. **Der Speicher ist eine Konstante** (`constant ROM : rom_t := (...)`). Die Daten sind zur Synthesezeit fest.
2. **Das Lesen ist getaktet.** Ein kombinatorisches ROM-Lesen kann nur auf LUTs abgebildet werden — das ist für ein winziges 16-Einträge-ROM in Ordnung, aber alles Größere braucht das getaktete Muster, damit der Synthesizer BRAM alloziert.
3. **`addr` wird rein als Index verwendet.** Keine teure kombinatorische Logik zwischen `addr` und dem Speicherzugriff.

## Warum dieser Inhalt?

Das Beispiel-ROM enthält `n²` für `n = 0..15`. Nützlich für eine Lookup-Tabellen-Quadrierungseinheit — schneller als ein voller Multiplizierer, wenn du nur kleine Operanden brauchst. Du könntest auch speichern:

- Eine Sinus-Tabelle (für DDS-Signalgeneratoren)
- CRC-Polynome
- Protokoll-Lookup-Tabellen
- Zeichensätze

## Was das Beispiel macht

Die Simulation treibt `addr` durch 5 bestimmte Werte: 0, 1, 4, 8, 15. Nach der Taktflanke zeigt `data` den entsprechenden ROM-Eintrag, einen Zyklus später (das ist die Leselatenz eines synchronen ROMs — du fragst die Adresse an, du bekommst die Daten beim **nächsten** Takt).

## Du bist dran

- **Mach es parametrisiert.** Ersetze die feste 16-Einträge-Größe mit `generic (DEPTH : integer := 16; WIDTH : integer := 8)`. Der Rest des Codes sollte weiterhin funktionieren.
- **Lese den Inhalt aus einer Datei.** Echte Projekte laden ROM-Inhalte über `init_from_file()` oder ein separates Paket mit Konstanten. Versuche, VHDL eine Textdatei mit Hex-Werten (`textio` + `hread`) zur Elaborationszeit lesen zu lassen.
- **Füge einen zweiten Leseport hinzu.** Für Dinge wie Instruktions-/Datenspeicher in einer Single-Cycle-CPU. Du wirst dieses Muster im späteren CPU-Projekt des Lehrplans sehen.
