# Mealy- vs. Moore-Automaten

Ein endlicher Automat (FSM) ist Steuerlogik, die sich merkt, was sie gerade tut. Formal ist er ein deterministischer Automat: eine endliche Menge von Zuständen, eine Übergangsfunktion, die aus dem aktuellen Zustand und den Eingängen den Folgezustand wählt, und eine Ausgangsfunktion. Jeder synchrone Zustandsautomat, den du baust, besteht aus drei Teilen:

- Einem **Zustandsregister** (eine Gruppe Flip-Flops) – das Einzige mit Gedächtnis.
- **Folgezustandslogik** (kombinatorisch, entscheidet, was der Zustand bei der nächsten Taktflanke sein wird).
- **Ausgangslogik** (kombinatorisch, entscheidet, was die Ausgänge gerade jetzt sind).

Wo die Ausgangslogik ihre Eingänge sucht, teilt Zustandsautomaten in zwei Familien.

## Moore: Ausgänge hängen nur vom Zustand ab

```mermaid
flowchart LR
    IN([input]) --> NS["next-state"]
    NS --> SR["state reg"]
    SR --> OUT["outputs"]
```

Bei gegebenem Zustand ist der Ausgang fix. Bist du in `S_GREEN`, dann `green = '1'`. Immer. Egal, wie der Eingang aussieht.

**Vorteile**
- Ausgänge ändern sich nur an Taktflanken → keine Glitches.
- Leicht nachzuvollziehen, leicht zu debuggen, leicht zu verifizieren.

**Nachteile**
- Mindestens ein Taktzyklus Latenz zwischen Eingang und Ausgang (der Zustand muss zuerst wechseln).
- Manchmal brauchst du mehr Zustände, um dasselbe Verhalten wie ein Mealy-Automat zu codieren.

## Mealy: Ausgänge hängen vom Zustand + Eingang ab

```mermaid
flowchart LR
    IN([input]) --> NS["next-state"]
    NS --> SR["state reg"]
    IN --> OL["output logic"]
    SR --> OL
    OL --> OUT([outputs])
```

Ausgänge sind eine Funktion des aktuellen Zustands **und** des aktuellen Eingangs. Sie können sich zwischen Taktflanken ändern.

**Vorteile**
- Schneller – Ausgänge können im selben Zyklus auf einen Eingang reagieren, nicht erst im nächsten.
- Oft weniger Zustände nötig.

**Nachteile**
- Ausgänge können glitchen (sie folgen dem kombinatorischen Eingang).
- Schwerer zu debuggen, wenn Ausgänge nicht mit Taktflanken zusammenfallen.

Ein schneller Weg, den Unterschied zu spüren: Dieselbe Aufgabe „erkenne eine `1` am Eingang" ist bei Mealy ein einziger Zustand (setze den Ausgang beim Übergang, der die `1` *sieht*), braucht bei Moore aber einen zusätzlichen „ich habe gerade eine 1 gesehen"-Zustand (damit der Ausgang daran gebunden werden kann, *in* diesem Zustand zu sein). Gleiches Verhalten, unterschiedliche Zustandszahl, im Timing um einen Zyklus versetzt.

## Ein, zwei oder drei Prozesse

VHDL gibt dir mehrere Wege, dieselbe Maschine zu schreiben. Sie synthetisieren zu derselben Hardware; sie unterscheiden sich in der Lesbarkeit und darin, wie leicht man sie falsch macht.

- **Ein-Prozess:** Zustandsregister *und* Ausgänge leben in einem getakteten Prozess. Alles endet registriert, also sind die Ausgänge von Natur aus glitch-frei (Moore-Stil, registriert). Kompakt, aber Folgezustands- und Ausgangslogik sind miteinander verwoben.
- **Zwei-Prozess (empfohlener Standard):** Ein getakteter Prozess hält das Zustandsregister; ein kombinatorischer Prozess berechnet `next_state` und die Ausgänge. Das ist das Muster, das die Kmila-Beispiele verwenden – es bildet sauber das obige Drei-Teile-Bild ab.
- **Drei-Prozess:** Zustandsregister, Folgezustandslogik und Ausgangslogik bekommen jeweils ihren eigenen Prozess. Am ausführlichsten, aber mit der klarsten Trennung und am schwersten versehentlich kaputtzumachen.

## Die eine Falle, in die jeder tappt: abgeleitete Latches

In einem *kombinatorischen* Prozess muss jedes Signal, das du treibst, auf **jedem** Pfad durch den Prozess einen Wert bekommen. Wenn ein Zweig eines `case` oder `if` vergisst, `next_state` (oder einen Ausgang) zuzuweisen, muss die Synthese den alten Wert *merken* – und das Einzige, was sich in kombinatorischer Logik etwas merkt, ist ein **Latch**. Latches willst du so gut wie nie: Sie sind pegelgesteuert, schwer zu timen und eine klassische Quelle für „läuft in der Sim, fällt im Silizium aus".

Die Lösung ist ein Einzeiler: Weise *jedem* Signal am Anfang des Prozesses einen **Default** zu, vor dem `case`.

```vhdl
process(state, input)
begin
  next_state <= state;   -- Default: stehen bleiben
  green      <= '0';     -- auch jeden Ausgang mit Default versehen
  case state is
    when S_IDLE  => if input = '1' then next_state <= S_RUN; end if;
    when S_RUN   => green <= '1';
                    next_state <= S_IDLE;
  end case;
end process;
```

Selbst wenn ein Zweig jetzt eine Zuweisung „vergisst", fängt der Default das ab – kein Latch. (Beachte, dass VHDL verlangt, dass `case`-Auswahlen erschöpfend sind, also fügst du oft ein `when others =>` hinzu; das stellt den Compiler zufrieden, aber es ist die *Default-Zuweisung*, die dich tatsächlich vor Latches rettet.)

## Zustandskodierung

Du schreibst benannte Zustände mit einem Aufzählungstyp und lässt das Werkzeug die Bits wählen:

```vhdl
type state_t is (S_IDLE, S_RUN, S_DONE);
signal state, next_state : state_t;
```

Der Synthesizer wählt eine Kodierung – **binär** (am wenigsten Flip-Flops), **One-Hot** (ein Flip-Flop pro Zustand, schnell und FPGA-freundlich) oder **Gray** (pro Schritt ändert sich ein Bit, gut für rauscharme Übergänge). Auf FPGAs ist One-Hot oft der Standard, weil Flip-Flops reichlich vorhanden sind und es die Folgezustandslogik flach hält. Du musst selten von Hand kodieren; lass das Werkzeug entscheiden, außer du hast einen Grund dagegen.

## Welchen solltest du nehmen?

**Standard: Moore.** Die Glitch-Freiheit und die kantenausgerichteten Ausgänge machen Designs vorhersagbar. Für Steuerlogik in einer CPU, einem Speichercontroller oder bei allem Sicherheitsrelevanten ist Moore die sicherere Wahl.

**Greife zu Mealy, wenn** du im selben Taktzyklus reagieren musst oder wenn die zusätzlichen Moore-Zustände den Entwurf aufblähen würden. UART und Protokoll-Engines landen oft bei Mealy wegen des Reaktionszeit-Arguments. Wenn dir Mealys Zustandszahl gefällt, du aber Moores sauberes Timing willst, **registriere die Mealy-Ausgänge** (schick sie durch ein weiteres Flip-Flop) – du bekommst glitch-freie Ausgänge zurück, zum Preis des einen Zyklus Latenz, den du eigentlich vermeiden wolltest.

## Bezug zum Kurs

Das ist das Herzstück von Einheit II, *Diseño de sistemas digitales*: der deterministische Automat mit und ohne Ausgang, die Mealy- und Moore-Modelle und ihre Beschreibung in HDL. Auf dem Papier baust du diese auch aus JK-/SR-/T-/D-Flip-Flops und verkleinerst sie mit Zustandsreduktions-Methoden (Primimplikanten / tabellarisch) – der Aufzählungstyp-Stil hier ist das HDL-Gegenstück zu genau diesem Zustandsdiagramm.

## Was du behalten solltest

- Moore: Ausgang ist eine Funktion des Zustands allein. Glitch-frei, ein Zyklus Latenz.
- Mealy: Ausgang ist eine Funktion von Zustand + Eingang. Schneller, kann glitchen – registriere die Ausgänge, falls das wichtig ist.
- Nutze das Zwei-Prozess-Muster und **gib jedem Signal** am Anfang des kombinatorischen Prozesses einen Default, um abgeleitete Latches zu vermeiden.
- Lass das Werkzeug die Zustandskodierung wählen (One-Hot ist auf FPGAs verbreitet); fang mit Moore an und wechsle aus einem konkreten Grund zu Mealy.
