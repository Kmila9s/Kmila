# Beispiel: abgeleiteter synchroner ROM

Jeder FPGA hat **Block-RAM** (BRAM) – dedizierte Speicherblöcke, die der Synthesizer verwendet, wenn er ein ROM- oder RAM-Muster in deinem Code erkennt. Du instanziierst keine Vendor-Primitive; du schreibst einfaches VHDL in einer Form, die das Tool mappen kann. „Inference" (Ableitung) ist der Name für diese Erkennung: Das Tool liest dein Array-und-Prozess und *leitet ab*, dass du einen Speicher gemeint hast.

Dieses Beispiel ist das portable Lehrbuch-ROM-Muster. Jeder ernsthafte Synthesizer (Xilinx/AMD Vivado, Intel/Altera Quartus, Lattice Radiant, das Open-Source-Yosys) erkennt es und ordnet automatisch BRAM zu.

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
2. **Der Read ist getaktet.** Ein kombinatorischer ROM-Read kann nur auf LUTs gemappt werden – bei einem winzigen 16-Eintrag-ROM okay, aber alles Größere braucht das getaktete Muster, damit der Synthesizer BRAM allokiert.
3. **`addr` wird rein als Index benutzt.** Keine teure kombinatorische Logik zwischen `addr` und dem Speicher-Lookup.

## Kombinatorisch vs. synchron, und die Lese-Latenz

Der getaktete Read ist das ganze Spiel. Schreib den Read als einfache nebenläufige Zuweisung –

```vhdl
data <= ROM(to_integer(unsigned(addr)));   -- kombinatorisch: verteiltes ROM (LUTs)
```

– und das Tool baut das ROM aus LUTs („verteilter" Speicher). Das ist null Lese-Latenz, kostet aber allgemeine Logik und skaliert nicht: Ein paar Kilobyte LUT-ROM ersäufen ein kleines Device. Registriere den Read (der `process` oben), und das Tool kann die ganze Tabelle in ein Block-RAM ablegen, was weit dichter ist.

Der Preis des registrierten Reads ist **ein Zyklus Latenz**: Du legst `addr` an einer Flanke an, und `data` erscheint an der *nächsten* Flanke. Das ist kein Bug – es ist das definierende Merkmal eines synchronen Speichers, und das interne Ausgangsregister des BRAM ist das, was den Entwurf bei hohen Taktraten das Timing einhalten lässt. Viele FPGAs bieten ein zweites, optionales Ausgangsregister für noch besseres Timing, zum Preis eines zweiten Zyklus Latenz.

## Die Adressbreite muss zur Tiefe passen

`to_integer(unsigned(addr))` darf niemals über das Array hinaus indizieren. Für ein 16-Eintrag-ROM sollte `addr` genau 4 Bit sein; eine breitere `addr` kann einen Index außerhalb des Bereichs erzeugen, der in der Simulation fehlschlägt und sich nach der Synthese unvorhersehbar verhält. Halte `2**addr'length = DEPTH`, oder begrenze/sichere den Index ab. Verbinde Tiefe, Breite und Adressgröße miteinander, und das Ganze bleibt konsistent.

## Warum dieser Inhalt?

Das Beispiel-ROM hält `n²` für `n = 0..15`. Nützlich für eine Lookup-Tabellen-Quadriereinheit – schneller als ein vollständiger Multiplizierer, wenn du nur kleine Operanden brauchst. Du könntest auch speichern:

- Eine Sinuswellen-Tabelle (für DDS-Signalgeneratoren)
- CRC-Polynome
- Protokoll-Lookup-Tabellen
- Zeichensätze (Fonts)

Ein ROM ist eigentlich nur eine Wahrheitstabelle, die in Silizium eingefroren ist: Jede kombinatorische Funktion der Adressbits kann vorberechnet und gespeichert werden – Logik gegen Speicher getauscht.

## Was das Beispiel tut

Die Sim treibt `addr` durch 5 spezifische Werte: 0, 1, 4, 8, 15. Nach der Taktflanke zeigt `data` den entsprechenden ROM-Eintrag, einen Zyklus später (das ist die Lese-Latenz eines synchronen ROMs – du fragst nach der Adresse, du bekommst die Daten beim **nächsten** Takt).

## Du bist dran

- **Mach es parametrisierbar.** Ersetze die feste 16-Eintrag-Größe durch `generic (DEPTH : integer := 16; WIDTH : integer := 8)` und dimensioniere `addr` als `std_logic_vector(log2(DEPTH)-1 downto 0)`. Der Rest des Codes sollte weiterhin funktionieren.
- **Lies den Inhalt aus einer Datei.** Echte Projekte laden ROM-Inhalte über eine Initialisierungsfunktion oder ein separates Konstanten-Package. Versuche, das VHDL eine Textdatei mit Hex-Werten lesen zu lassen (`std.textio` + `hread`) – in einer `impure function`, die das Array zurückgibt; die meisten Tools führen sie zur Elaborationszeit aus und backen das Ergebnis ins BRAM.
- **Füge einen zweiten Read-Port hinzu.** Für Dinge wie Instruction-/Data-Memory in einer Single-Cycle-CPU. Du siehst dieses Muster später im Curriculum im CPU-Projekt.

## Bezug zum Kurs

Das ist Einheit IV, *Memorias*: die ROM-Familie (ROM, PROM, EPROM, EEPROM, Flash) und speziell *manejo de arreglos y diseño de memorias con lenguaje de descripción de hardware*. Die `type … array … of`-Konstante ist genau das „Array-Handling", das die Einheit verlangt; die Datei-Initialisierungs-Übung greift auf Einheit Vs Arbeit mit Dateien, Funktionen und Packages voraus.

## Was du behalten solltest

- Ableitung (Inference) bedeutet, Speicher in einer Form zu schreiben, die das Tool erkennt – keine Vendor-Primitive nötig.
- Ein **Konstanten**-Array, das in einem getakteten Prozess gelesen wird, ist das portable synchrone-ROM-Muster; es mappt auf Block-RAM.
- Getakteter Read = ein Zyklus Latenz und dichtes BRAM; kombinatorischer Read = null Latenz, aber LUT-hungrig und nicht skalierbar.
- Passe die Adressbreite an die Tiefe an, damit der Index nie über das Ende des Arrays hinausläuft.
