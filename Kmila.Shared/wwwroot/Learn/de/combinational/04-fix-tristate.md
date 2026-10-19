# Korrigieren: Der Tristate-Buffer, der immer treibt

Ein Tristate-Buffer ist ein umschaltbarer Ausgang. Wenn das Output-Enable-Signal (`oe`) high ist, gibt er seine Daten an den Bus weiter. Bei low **gibt er den Bus frei** — er treibt hochohmig (`'Z'`), damit andere Geräte reden können. Es ist die eine Stelle in dieser Einheit, wo sich der `'Z'`-Wert aus der neunwertigen Logik seinen Platz verdient.

## Was falsch ist

Öffne das Beispiel und sieh dir den Body an:

```vhdl
y <= d when oe = '0' else 'Z';
```

Lies es laut vor: „`y` ist `d`, **wenn `oe` 0 ist**, sonst `'Z'`.“ Das ist falsch herum. Du willst, dass der Buffer Daten weitergibt, wenn das Enable **high** ist, und freigibt, wenn es **low** ist.

## Dein Auftrag

- Öffne das Beispiel im Editor.
- Korrigiere die Bedingung der `y <=`-Zuweisung, sodass der Sinn von `oe` stimmt:

```vhdl
y <= d when oe = '1' else 'Z';
```

- Lass die Simulation laufen und prüfe im Wellenform-Tab:
  - Bei `oe = '0'` soll `y` `'Z'` sein.
  - Bei `oe = '1'` soll `y` `d` folgen.

## Warum `'Z'` hier besonders ist

`'Z'` ist kein dritter Logik*pegel* — es bedeutet „ich treibe nicht“. Auf dem Waveform zeigt GTKWave es als eine Linie, die in der Mitte parkt, weder high noch low. Der ganze Sinn eines Tristate ist, einem Draht **mehrere** mögliche Treiber zu erlauben, während nur einer zur Zeit aktiv ist. Das ist nur sicher, weil `std_logic` ein *aufgelöster* (resolved) Typ ist: Wenn ein Treiber `'Z'` ist und ein anderer `'0'` oder `'1'` treibt, lässt die Auflösung den starken Wert gewinnen. Wenn jeder Treiber `'Z'` ist, floatet der Draht.

## Extra: pass auf `'X'` auf

Wenn zwei Tristate-Buffer denselben Bus gleichzeitig mit **unterschiedlichen** Werten treiben, würde eine echte Schaltung gegen sich selbst kämpfen und überhitzen. Die Auflösungsfunktion modelliert das als `'X'` (stark unbekannt) auf dem Waveform — ein klares Warnsignal, dass deine Enables sich überlappen. Bau eine winzige Testbench mit zwei deiner Buffer, die mit demselben `y` verdrahtet sind, schalte beide `oe`-Leitungen mit unterschiedlichem `d` ein und beobachte, wie der Bus auf `'X'` geht.

## Warum Tristate wichtig ist (und wo es heute lebt)

Tristate-Buffer sind die Art, wie mehrere Treiber sich einen einzigen Draht (einen Bus) teilen, ohne sich kurzzuschließen. Ein wichtiger Vorbehalt für die Hardware, die du tatsächlich ansteuern wirst: **FPGAs haben keine internen Tristate-Buffer** in ihrem Logik-Gewebe — die Synthese ersetzt einen internen Tristate-Bus automatisch durch Multiplexer. Tristates überleben nur an den **I/O-Pads**, wo sie genau so funktionieren, wie bidirektionale Pins arbeiten: DDR-Speicher-Datenleitungen, I²C-`sda` und andere geteilte Busse schalten den Pin-Treiber alle mit einem `oe` ein und aus. Dieses bidirektionale Pad wird mit einem `inout`-Port modelliert:

```vhdl
io_pin <= drive_value when oe = '1' else 'Z';  -- das Pad treiben oder freigeben
read_value <= io_pin;                           -- immer abtasten, was da ist
```

## Was du behalten solltest

- Ein Tristate gibt Daten weiter, wenn er aktiviert ist, und treibt `'Z'` (freigegeben), wenn nicht — bring den Sinn von `oe` richtig hin.
- `'Z'` bedeutet „treibt nicht“, was es vielen Treibern erlaubt, sich einen aufgelösten `std_logic`-Draht zu teilen.
- Zwei aktive Treiber mit unterschiedlichen Werten lösen sich zu `'X'` auf — ein Bug, den du auf dem Waveform siehst.
- Innerhalb eines FPGA werden Tristates zu MUXes; echte Tristates leben an den I/O-Pads für bidirektionale Pins (`inout`).
