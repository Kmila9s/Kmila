// VHDL-Sprachdokumentation — Deutsch.
// Gleiche Struktur wie vhdl-data.en.js.

(function () {
    window.kmilaVhdlDocs = window.kmilaVhdlDocs || {};
    window.kmilaVhdlDocs.de = {

        keywords: {
            // --- Strukturelle Deklarationen ---
            entity:       { summary: "Deklariert die externe Schnittstelle eines Hardware-Moduls.", description: "Eine **entity** benennt eine Entwurfseinheit und listet ihre Ports (Ein-/Ausgänge) und Generics (Parameter) auf. Sie ist die Black-Box-Sicht — eine architecture liefert die Implementierung.", example: "entity counter is\n  port (\n    clk : in  std_logic;\n    q   : out std_logic_vector(7 downto 0)\n  );\nend entity counter;" },
            architecture: { summary: "Implementierungs-Rumpf einer entity.", description: "Eine **architecture** liefert eine konkrete Implementierung für eine entity. Eine entity kann mehrere architectures haben (z. B. `behavioral`, `rtl`, `structural`).", example: "architecture behavioral of counter is\n  signal cnt : unsigned(7 downto 0) := (others => '0');\nbegin\n  process(clk)\n  begin\n    if rising_edge(clk) then cnt <= cnt + 1; end if;\n  end process;\n  q <= std_logic_vector(cnt);\nend architecture behavioral;" },
            component:    { summary: "Instanziierbare Kopie der Schnittstelle einer entity.", description: "Eine **component**-Deklaration erlaubt einer architecture, eine andere entity zu instanziieren. Vor der modernen direkten Entity-Instanziierung erforderlich.", example: "component fifo is\n  port ( clk : in std_logic; din : in std_logic_vector(7 downto 0) );\nend component;" },
            configuration:{ summary: "Bindet eine Komponente an ein bestimmtes entity/architecture-Paar.", description: "Selten in synthetisierbarem Code; meist für Simulation, wenn mehrere architectures für eine entity existieren." },
            package:      { summary: "Wiederverwendbare Sammlung von Typen, Konstanten und Unterprogrammen.", description: "Ein **package** gruppiert Deklarationen, die per `use` in andere Entwurfseinheiten importiert werden können.", example: "package my_pkg is\n  constant WIDTH : integer := 8;\n  type state_t is (idle, busy, done);\nend package my_pkg;" },
            body:         { summary: "Implementierungsteil eines package.", description: "`package body` enthält die Implementierungen der in `package` deklarierten Unterprogramme." },

            // --- Bibliotheken & use-Klauseln ---
            library:      { summary: "Macht eine Bibliothek sichtbar.", description: "Importiert eine VHDL-Bibliothek. `ieee` ist fast immer das, was man für `std_logic_1164` und `numeric_std` will.", example: "library ieee;" },
            use:          { summary: "Macht den Inhalt eines package sichtbar.", description: "Importiert Namen aus einem package, sodass sie ohne Qualifizierung verwendet werden können.", example: "use ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;" },

            // --- Ports & Generics ---
            port:         { summary: "Deklariert die Ports einer entity (Signale, die die Grenze überqueren).", description: "Erscheint innerhalb einer entity. Jeder Port hat einen Namen, eine Richtung (`in`, `out`, `inout`, `buffer`) und einen Typ.", example: "port (\n  clk  : in  std_logic;\n  data : out std_logic_vector(7 downto 0)\n);" },
            generic:      { summary: "Deklariert Compile-Zeit-Parameter für eine entity.", description: "Generics sind wie Template-Parameter — der Instanziierende wählt ihren Wert, und der Entwurf skaliert (Busbreiten, FIFO-Tiefen).", example: "generic ( WIDTH : integer := 8 );" },
            map:          { summary: "Verbindet Instanz-Ports/Generics mit den umgebenden Signalen.", description: "Verwendet in `port map (...)` und `generic map (...)` bei Komponenten-Instanziierungen.", example: "u_counter : counter port map (\n  clk => sys_clk,\n  q   => led\n);" },
            in:           { summary: "Port-Richtung — nur Eingang.", description: "Der Entwurf liest diesen Port; er kann ihn nicht treiben." },
            out:          { summary: "Port-Richtung — nur Ausgang.", description: "Der Entwurf treibt diesen Port; sein Wert kann innerhalb derselben entity nicht zurückgelesen werden (verwende `buffer` oder ein internes Signal, wenn nötig)." },
            inout:        { summary: "Bidirektionaler Port.", description: "Treibt und liest dieselbe physische Leitung. Nur ein Treiber darf gleichzeitig aktiv sein — die anderen müssen `'Z'` (hochohmig) sein." },
            buffer:       { summary: "Ausgang, der auch intern gelesen werden kann.", description: "Wie `out`, aber auch innerhalb der entity lesbar. Gilt als veraltet — bevorzuge `out` mit einem internen Signal." },

            // --- Signal-Deklarationen ---
            signal:       { summary: "Deklariert ein Signal (Leitung).", description: "Ein **signal** transportiert Werte über die Zeit. Innerhalb eines process planen Signal-Zuweisungen (`<=`) die Aktualisierung, statt sie sofort anzuwenden.", example: "signal counter : unsigned(7 downto 0) := (others => '0');" },
            variable:     { summary: "Deklariert eine Variable (process-lokal).", description: "Eine **variable** lebt innerhalb eines process oder Unterprogramms. Im Gegensatz zu einem Signal wirkt `:=` sofort — nützlich für Zwischenrechnungen innerhalb eines process.", example: "process\n  variable tmp : integer := 0;\nbegin\n  tmp := tmp + 1;\nend process;" },
            constant:     { summary: "Deklariert eine benannte Konstante.", description: "Gibt einem festen Wert einen lesbaren Namen. Kann in jedem Gültigkeitsbereich erscheinen.", example: "constant CLOCK_HZ : integer := 50_000_000;" },
            shared:       { summary: "Geteilte Variable für mehrere Prozesse.", description: "Nur in VHDL-2008 sinnvoll. Vorsichtig verwenden — typischerweise geschützte Typen oder Integer." },

            // --- Typ-System ---
            type:         { summary: "Definiert einen neuen Typ.", description: "Wird für Aufzählungen, Bereiche, Arrays und Records verwendet.", example: "type state_t is (idle, run, done);" },
            subtype:      { summary: "Deklariert eine eingeschränkte Version eines existierenden Typs.", description: "Fügt Bereichs- oder Auflösungs-Einschränkungen hinzu, ohne einen grundlegend neuen Typ zu schaffen.", example: "subtype byte is std_logic_vector(7 downto 0);" },
            array:        { summary: "Deklariert einen Array-Typ.", description: "Ein- oder mehrdimensional. Kann eingeschränkt (feste Größe) oder uneingeschränkt (Größe bei Verwendung festgelegt) sein.", example: "type memory_t is array(0 to 255) of std_logic_vector(7 downto 0);" },
            record:       { summary: "Deklariert einen zusammengesetzten Typ mit benannten Feldern.", description: "VHDL-Pendant zum C-`struct`.", example: "type axi_cmd_t is record\n  addr : std_logic_vector(31 downto 0);\n  len  : unsigned(7 downto 0);\nend record;" },
            range:        { summary: "Bereichs-Einschränkung für Skalartypen.", description: "Typischerweise `A to B` (aufsteigend) oder `A downto B` (absteigend). Auch in for-Schleifen verwendet.", example: "for i in 0 to 7 loop ... end loop;" },
            downto:       { summary: "Absteigende Bereichsrichtung.", description: "`MSB downto LSB` — Standard-Konvention für Vektor-Indizes, sodass Bit 7 links steht.", example: "std_logic_vector(7 downto 0)" },
            to:           { summary: "Aufsteigende Bereichsrichtung.", description: "`A to B` — verwendet in Schleifen und aufwärts indizierten Arrays.", example: "for i in 0 to 15 loop ... end loop;" },
            others:       { summary: "Wildcard für Aggregat-Vorgaben.", description: "Setzt alle nicht benannten Positionen in einem Aggregat.", example: "signal zeros : std_logic_vector(7 downto 0) := (others => '0');" },
            units:        { summary: "Deklariert die Einheiten eines physischen Typs.", description: "Wird beim Definieren eigener physischer Typen wie `time` verwendet." },
            access:       { summary: "Pointer-artiger Zugriffstyp.", description: "Selten synthetisierbar — für verkettete Datenstrukturen in der Simulation." },
            file:         { summary: "Datei-Typ für E/A während der Simulation.", description: "Nicht synthetisierbar. Üblich in Testbenches zum Lesen von Stimulus-Dateien." },

            // --- Sequenzielle Anweisungen / Kontrollfluss ---
            process:      { summary: "Sequenzieller Anweisungsblock.", description: "Ein **process** führt seine Anweisungen sequenziell aus und läuft erneut, sobald sich ein Signal aus seiner Sensitivitätsliste ändert. Das Brot-und-Butter-Konstrukt für getaktete Logik.", example: "process(clk, reset)\nbegin\n  if reset = '1' then\n    q <= (others => '0');\n  elsif rising_edge(clk) then\n    q <= d;\n  end if;\nend process;" },
            if:           { summary: "Bedingte Ausführung.", description: "Standard-Struktur `if … then … elsif … else … end if;` innerhalb eines process.", example: "if rising_edge(clk) then\n  q <= d;\nend if;" },
            then:         { summary: "Trennt Bedingung und Rumpf in `if` / `elsif`.", description: "" },
            elsif:        { summary: "Zusätzlicher bedingter Zweig in einem `if`.", description: "" },
            else:         { summary: "Fallback-Zweig in `if` / `when`.", description: "" },
            case:         { summary: "Mehrweg-Verzweigung anhand eines Wertes.", description: "Wie `switch` in C. Oft sauberer als eine lange `if`/`elsif`-Kette.", example: "case state is\n  when idle => …\n  when busy => …\n  when others => null;\nend case;" },
            when:         { summary: "Zweig-Selektor in `case`, auch in bedingter Signal-Zuweisung.", description: "", example: "q <= '1' when enable = '1' else '0';" },
            select:       { summary: "Wählt zwischen Alternativen in einer selektiven Signal-Zuweisung.", description: "Nebenläufige `with … select`-Anweisung, eine der zwei klassischen nebenläufigen bedingten Formen.", example: "with state select\n  q <= a when idle,\n       b when busy,\n       c when others;" },
            loop:         { summary: "Generisches Schleifen-Konstrukt.", description: "Kann unbedingt (Endlos-), `for`-indiziert oder `while`-bedingt sein.", example: "for i in 0 to 7 loop\n  result(i) <= data(7 - i);\nend loop;" },
            for:          { summary: "Indizierte Schleife.", description: "Iteriert über einen Bereich. Nützlich sowohl in processes als auch in `generate`-Blöcken.", example: "for i in 0 to 15 loop … end loop;" },
            while:        { summary: "Bedingungsbasierte Schleife.", description: "Selten synthetisierbar, wenn die Bedingung von Signalen abhängt." },
            next:         { summary: "Springt zur nächsten Iteration einer Schleife.", description: "" },
            exit:         { summary: "Bricht aus einer Schleife aus.", description: "" },
            return:       { summary: "Gibt einen Wert aus einer Funktion zurück oder beendet eine Prozedur.", description: "" },
            wait:         { summary: "Setzt einen process aus.", description: "`wait on <Signale>` / `wait until <Bed>` / `wait for <Zeit>`. Nur in bestimmten Formen synthetisierbar (z. B. `wait until rising_edge(clk)`).", example: "wait until rising_edge(clk);" },
            null:         { summary: "Leeranweisung.", description: "Platzhalter, wo syntaktisch eine Anweisung erforderlich ist." },

            // --- Nebenläufig / generate ---
            begin:        { summary: "Beginn eines Anweisungsblocks.", description: "Öffnet den Rumpf von `architecture`, `process`, `function`, `procedure`, `generate`." },
            end:          { summary: "Schließt einen Block.", description: "Paart sich mit `entity`, `architecture`, `process`, `if`, `case`, `loop` usw." },
            generate:     { summary: "Erzeugt Hardware bedingt oder in Schleifen.", description: "Stell es dir vor wie eine Compile-Zeit-for-Schleife oder ein Compile-Zeit-if, das replizierte oder bedingte Struktur ausgibt.", example: "gen_bits : for i in 0 to 7 generate\n  q(i) <= d(7 - i);\nend generate;" },
            block:        { summary: "Gekapselte nebenläufige Anweisungsregion.", description: "Gruppiert Anweisungen unter einer Guard-Bedingung. In modernem Code selten." },
            assert:       { summary: "Simulationszeit-Prüfung.", description: "Löst einen `report` aus und stoppt (oder nicht, je nach Severity), wenn die Bedingung falsch ist. Nicht synthetisierbar.", example: "assert data'length = 8 report \"falsche Breite\" severity error;" },
            report:       { summary: "Gibt eine Simulationsmeldung aus.", description: "Oft mit `assert` kombiniert. Severity: `note`, `warning`, `error`, `failure`." },
            severity:     { summary: "Severity-Level für `assert`/`report`.", description: "" },

            // --- Unterprogramme ---
            function:     { summary: "Reines Unterprogramm, das einen Wert zurückgibt.", description: "Berechnet einen Wert aus seinen Argumenten — keine Seiteneffekte auf Signale.", example: "function max(a, b : integer) return integer is\nbegin\n  if a > b then return a; else return b; end if;\nend function;" },
            procedure:    { summary: "Unterprogramm, das Signale treiben kann.", description: "Anders als eine function kann eine procedure `out`/`inout`-Parameter haben." },
            impure:       { summary: "Markiert eine function, die von externem Zustand abhängen darf.", description: "Erlaubt das Lesen von Signalen, die außerhalb ihres Bereichs deklariert sind." },
            pure:         { summary: "Markiert eine function ohne externe Abhängigkeiten.", description: "Standard für `function` — selten ausgeschrieben." },
            open:         { summary: "Lässt einen Port unverbunden oder öffnet eine Datei.", description: "In einer port map: `some_port => open;`. Für Datei-E/A: `file F : text open read_mode is ...;`" },

            // --- Operatoren (Schlüsselwörter) ---
            and:          { summary: "Logisches UND.", description: "Element-weise auf std_logic_vector / bit_vector." },
            or:           { summary: "Logisches ODER.", description: "Element-weise auf Vektoren." },
            not:          { summary: "Logisches NICHT.", description: "Unäre Negation." },
            nand:         { summary: "Logisches NAND.", description: "" },
            nor:          { summary: "Logisches NOR.", description: "" },
            xor:          { summary: "Logisches XOR.", description: "" },
            xnor:         { summary: "Logisches XNOR.", description: "" },
            mod:          { summary: "Modulo.", description: "Ergebnis hat das Vorzeichen des Divisors (anders als `rem`)." },
            rem:          { summary: "Rest.", description: "Ergebnis hat das Vorzeichen des Dividenden." },
            abs:          { summary: "Absolutwert.", description: "" },
            sla:          { summary: "Arithmetisches Linksschieben.", description: "" },
            sra:          { summary: "Arithmetisches Rechtsschieben.", description: "" },
            sll:          { summary: "Logisches Linksschieben.", description: "" },
            srl:          { summary: "Logisches Rechtsschieben.", description: "" },
            rol:          { summary: "Linksrotieren.", description: "" },
            ror:          { summary: "Rechtsrotieren.", description: "" },

            // --- Verschiedenes ---
            is:           { summary: "Trenn-Schlüsselwort.", description: "Erscheint in `entity X is`, `architecture Y of X is`, `type T is` usw." },
            of:           { summary: "Trenn-Schlüsselwort.", description: "Erscheint in `architecture A of X`, `array(…) of …`." },
            all:          { summary: "Wildcard.", description: "Verwendet in `use x.y.all;` und in Attribut-/Sensitivitätslisten-Kontexten." },
            new:          { summary: "Allokiert ein Access-Typ-Objekt (Pointer).", description: "Selten synthetisierbar." },
            label:        { summary: "Versieht eine Anweisung mit einem Namen.", description: "Nützlich für processes, generates und Schleifen, um sich später darauf zu beziehen." },
            alias:        { summary: "Gibt einem Objekt oder Teil davon einen alternativen Namen.", description: "", example: "alias sign : std_logic is data(7);" },
            attribute:    { summary: "Metadaten-Tag oder vordefinierte Abfrage.", description: "Abfragen wie `x'length`, `x'range`, `signal'event`." },
            group:        { summary: "Gruppiert benannte Elemente für die Anwendung von Attributen.", description: "Selten gesehen." },
            bus:          { summary: "Veralteter Signal-Modifizierer.", description: "Historisch." },
            register:     { summary: "Veralteter Signal-Modifizierer.", description: "Historisch." },
            disconnect:   { summary: "Steuert die Diskonnektionszeit auf guarded signals.", description: "Historisch / selten." },
            after:        { summary: "Transport-Verzögerungs-Spezifikator.", description: "`a <= b after 5 ns;` — eine reine Simulationsverzögerung." },
            reject:       { summary: "Inertial-Verzögerungs-Verwerfungs-Puls.", description: "" },
            inertial:     { summary: "Inertial-Verzögerung (Standard).", description: "Filtert kurze Pulse." },
            transport:    { summary: "Transport-Verzögerung (ohne Filter).", description: "" },
            guarded:      { summary: "Qualifiziert einen Block oder eine Signal-Zuweisung.", description: "Selten." },
            linkage:      { summary: "Port-Richtung für sprachübergreifendes Linken.", description: "Nur relevant in gemischten VHDL/Verilog-Flüssen." },
            postponed:    { summary: "Verzögert einen process oder eine Assertion bis zum Ende des Delta-Zyklus.", description: "Nur Simulation." },
            literal:      { summary: "Attribut-bezogenes Schlüsselwort.", description: "Selten." },
            unaffected:   { summary: "'Mache nichts'-Platzhalter in einer bedingten Signal-Zuweisung.", description: "" },
            until:        { summary: "Verwendet mit `wait until <Bedingung>`.", description: "" },
            with:         { summary: "Beginnt eine selektive Signal-Zuweisung.", description: "`with expr select …`." },
            on:           { summary: "Verwendet in `wait on <Signal-Liste>`.", description: "" }
        },

        types: {
            std_logic:        { summary: "IEEE 9-wertige Logik: eine Leitung.", description: "Werte: `'0'`, `'1'`, `'Z'` (hochohmig), `'X'` (unbekannt), `'U'` (uninitialisiert), `'W'`, `'L'`, `'H'`, `'-'` (don't-care). Standard für 1-Bit-Signale in der Synthese.", example: "signal clk : std_logic := '0';" },
            std_logic_vector: { summary: "Vektor von std_logic — ein Bus.", description: "Mit Bereich deklariert: `(high downto low)` ist die übliche Konvention. Gut für Verkabelung; für Arithmetik in `unsigned`/`signed` umwandeln.", example: "signal data : std_logic_vector(7 downto 0);" },
            std_ulogic:       { summary: "Unaufgelöste Variante von std_logic.", description: "Nur ein Treiber zulässig. Selten direkt verwendet." },
            std_ulogic_vector:{ summary: "Unaufgelöster Vektor von std_ulogic.", description: "" },
            bit:              { summary: "Einfache 2-wertige Logik: `'0'` oder `'1'`.", description: "Älter als std_logic. Verwende std_logic in der Synthese — bit kann Z, X usw. nicht abbilden." },
            bit_vector:       { summary: "Vektor von `bit`.", description: "" },
            boolean:          { summary: "Boolescher Typ: `true` / `false`.", description: "In Bedingungen verwendet; nicht direkt auf eine Leitung synthetisierbar (in std_logic umwandeln)." },
            integer:          { summary: "Ganzzahl — mindestens 32-Bit signiert.", description: "Hervorragend für Zähler und Indizes. Mit Teilbereich einschränken (z. B. `integer range 0 to 255`), damit die Synthese die Bitbreite kennt." },
            natural:          { summary: "`integer range 0 to integer'high`.", description: "Nicht-negative Ganzzahl." },
            positive:         { summary: "`integer range 1 to integer'high`.", description: "Strikt positive Ganzzahl." },
            real:             { summary: "Fließkomma-Typ.", description: "Nicht synthetisierbar — nur Simulation/Konstanten." },
            character:        { summary: "ASCII-/ISO-8859-1-Zeichen.", description: "" },
            string:           { summary: "Array von character.", description: "Hauptsächlich für `report`-Meldungen und Datei-E/A." },
            time:             { summary: "Physischer Typ für Zeit.", description: "Einheiten: `fs`, `ps`, `ns`, `us`, `ms`, `sec`, `min`, `hr`. Nur Simulation." },
            signed:           { summary: "Numerischer Vektor (signiert) aus ieee.numeric_std.", description: "Für signierte Arithmetik. Konvertierung von std_logic_vector über den `signed()`-Cast.", example: "signal x : signed(7 downto 0);" },
            unsigned:         { summary: "Numerischer Vektor (unsigniert) aus ieee.numeric_std.", description: "Für unsignierte Arithmetik. Cast über `unsigned()` / `std_logic_vector()`.", example: "signal cnt : unsigned(7 downto 0) := (others => '0');" },
            line:             { summary: "Zugriff auf eine Textzeile — für Datei-E/A.", description: "Verwendet mit `readline` / `writeline` in Testbenches." },
            text:             { summary: "Datei-Typ aus dem textio-Package.", description: "" },
            severity_level:   { summary: "Severity-Level für `assert`/`report`.", description: "Werte: `note`, `warning`, `error`, `failure`." },
            side:             { summary: "Textio-Hilfstyp.", description: "" },
            width:            { summary: "Textio-Hilfstyp.", description: "" }
        },

        builtins: {
            rising_edge:    { summary: "Wahr bei einem 0→1-Übergang eines Takts.", signature: "rising_edge(s : std_ulogic) return boolean", description: "Bevorzugt gegenüber `clk'event and clk = '1'`. Funktioniert für std_logic und std_ulogic.", example: "if rising_edge(clk) then q <= d; end if;" },
            falling_edge:   { summary: "Wahr bei einem 1→0-Übergang.", signature: "falling_edge(s : std_ulogic) return boolean", description: "Gegenstück zu `rising_edge`." },
            to_integer:     { summary: "Wandelt signed/unsigned in integer um.", signature: "to_integer(arg : unsigned) return natural", description: "Aus `ieee.numeric_std`. Üblicher Cast nach Arithmetik.", example: "variable n : integer := to_integer(unsigned(data));" },
            to_unsigned:    { summary: "Wandelt integer in unsigned-Vektor um.", signature: "to_unsigned(arg : natural; size : natural) return unsigned", description: "", example: "counter <= to_unsigned(42, 8);" },
            to_signed:      { summary: "Wandelt integer in signed-Vektor um.", signature: "to_signed(arg : integer; size : natural) return signed", description: "" },
            resize:         { summary: "Ändert die Größe eines signed/unsigned-Vektors.", signature: "resize(arg : unsigned; new_size : natural) return unsigned", description: "Füllt unsigned mit Nullen, signed mit Vorzeichenerweiterung." },
            shift_left:     { summary: "Logisches Linksschieben.", signature: "shift_left(arg : unsigned; count : natural) return unsigned", description: "Füllt rechts mit Nullen." },
            shift_right:    { summary: "Logisches Rechtsschieben für unsigned / arithmetisches für signed.", signature: "shift_right(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_left:    { summary: "Zirkuläres Linksschieben.", signature: "rotate_left(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_right:   { summary: "Zirkuläres Rechtsschieben.", signature: "rotate_right(arg : unsigned; count : natural) return unsigned", description: "" },
            conv_integer:   { summary: "Legacy-Integer-Konvertierung.", signature: "conv_integer(arg : std_logic_vector) return integer", description: "Aus `ieee.std_logic_arith`. In neuem Code `to_integer` aus numeric_std bevorzugen." },
            conv_std_logic_vector: { summary: "Legacy-Vektor-Konvertierung.", signature: "conv_std_logic_vector(arg : integer; size : integer) return std_logic_vector", description: "In neuem Code `std_logic_vector(to_unsigned(...))` bevorzugen." },
            now:            { summary: "Aktuelle Simulationszeit.", signature: "now : time", description: "Nur Simulation." },
            readline:       { summary: "Liest eine Zeile aus einer textio-Datei.", signature: "readline(file f : text; l : out line)", description: "Testbench-E/A." },
            writeline:      { summary: "Schreibt eine Zeile in eine textio-Datei.", signature: "writeline(file f : text; l : inout line)", description: "Testbench-E/A." },
            endline:        { summary: "Zeilenende-Prüfung.", signature: "endline(l : inout line) return boolean", description: "Textio-Hilfsfunktion." },
            endfile:        { summary: "Dateiende-Prüfung.", signature: "endfile(file f : text) return boolean", description: "" },
            write:          { summary: "Textio-Schreibprimitive.", signature: "write(l : inout line; value : … )", description: "" },
            read:           { summary: "Textio-Leseprimitive.", signature: "read(l : inout line; value : out … )", description: "" }
        },

        snippets: {
            entity: {
                label: "entity (Gerüst)",
                summary: "Minimale entity-Deklaration einfügen",
                insertText: "entity ${1:name} is\n\tport (\n\t\t${2:clk}   : in  std_logic;\n\t\t${3:reset} : in  std_logic;\n\t\t${4:q}     : out std_logic_vector(${5:7} downto 0)\n\t);\nend entity ${1:name};\n\narchitecture behavioral of ${1:name} is\nbegin\n\t$0\nend architecture behavioral;"
            },
            process_clocked: {
                label: "process (getaktet, asynchroner Reset)",
                summary: "Standard-getakteter process mit asynchronem Reset",
                insertText: "process(${1:clk}, ${2:reset})\nbegin\n\tif ${2:reset} = '1' then\n\t\t$3 <= (others => '0');\n\telsif rising_edge(${1:clk}) then\n\t\t$0\n\tend if;\nend process;"
            },
            process_combinational: {
                label: "process (kombinatorisch)",
                summary: "Kombinatorischer process mit Sensitivitätsliste",
                insertText: "process(${1:all})\nbegin\n\t$0\nend process;"
            },
            if_else: {
                label: "if … elsif … else",
                summary: "Drei-Wege-Bedingung",
                insertText: "if ${1:condition} then\n\t$2\nelsif ${3:other} then\n\t$4\nelse\n\t$5\nend if;"
            },
            case_stmt: {
                label: "case … when … end case",
                summary: "Gerüst für case-Anweisung",
                insertText: "case ${1:expr} is\n\twhen ${2:value} => $3\n\twhen others    => $0\nend case;"
            },
            for_loop: {
                label: "for i in 0 to N-1 loop",
                summary: "Indizierte Schleife",
                insertText: "for ${1:i} in 0 to ${2:N}-1 loop\n\t$0\nend loop;"
            },
            signal: {
                label: "Signal-Deklaration",
                summary: "Ein Signal deklarieren",
                insertText: "signal ${1:name} : ${2:std_logic_vector}(${3:7} downto 0) := (others => '0');"
            },
            counter: {
                label: "Zähler-Vorlage",
                summary: "Zähler mit Wrap-Around",
                insertText: "signal cnt : unsigned(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk}, ${3:reset})\nbegin\n\tif ${3:reset} = '1' then\n\t\tcnt <= (others => '0');\n\telsif rising_edge(${2:clk}) then\n\t\tcnt <= cnt + 1;\n\tend if;\nend process;\n${4:q} <= std_logic_vector(cnt);"
            },
            shift_register: {
                label: "Schieberegister-Vorlage",
                summary: "N-Bit-Schieberegister",
                insertText: "signal sr : std_logic_vector(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk})\nbegin\n\tif rising_edge(${2:clk}) then\n\t\tsr <= sr(${1:7}-1 downto 0) & ${3:d};\n\tend if;\nend process;\n${4:q} <= sr;"
            },
            library_ieee: {
                label: "library ieee + use",
                summary: "Standard-IEEE-Importe",
                insertText: "library ieee;\nuse ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;\n\n$0"
            }
        },

        ui: {
            declaredOnLine: "deklariert in Zeile",
            kindSignal:     "Signal",
            kindVariable:   "Variable",
            kindConstant:   "Konstante",
            kindPort:       "Port",
            categoryKeyword:  "VHDL-Schlüsselwort",
            categoryType:     "Typ",
            categoryBuiltin:  "IEEE-Funktion",
            categoryUser:     "Benutzerdeklaration",
            categorySnippet:  "Snippet",
            translationPending: "⚠ Übersetzung ausstehend — Beschreibung auf Englisch."
        }
    };
})();
