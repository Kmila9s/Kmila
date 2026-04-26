# Fix it: Der Tristate-Puffer, der immer treibt

Ein Tristate-Puffer ist ein schaltbarer Ausgang. Wenn das Output-Enable-Signal (`oe`) hoch ist, reicht er seine Daten auf den Bus durch. Wenn `oe` niedrig ist, **gibt er den Bus frei** — er treibt hochohmig (`'Z'`), damit andere Geräte reden können.

## Was falsch ist

Öffne das Beispiel und schau dir den Rumpf an:

```vhdl
y <= d when oe = '0' else 'Z';
```

Lies es laut vor: "`y` ist gleich `d`, **wenn `oe` null ist**, sonst `'Z'`." Das ist verkehrt herum. Du willst, dass der Puffer Daten durchreicht, wenn das Enable **hoch** ist, und freigibt, wenn es **niedrig** ist.

## Deine Mission

- Öffne das Beispiel im Editor.
- Korrigiere die Bedingung in Zeile 12 (die `y <=`-Zuweisung).
- Führe die Simulation aus und verifiziere im Waveforms-Tab:
  - Wenn `oe = '0'`, sollte `y` gleich `'Z'` sein.
  - Wenn `oe = '1'`, sollte `y` `d` folgen.

## Warum Tristate wichtig ist

Tristate-Puffer sind die Art, wie sich mehrere Treiber einen einzelnen Draht (einen Bus) teilen, ohne sich gegenseitig kurzzuschließen. FPGAs haben in ihrer internen Struktur keine Tristates — sie verwenden stattdessen Multiplexer — aber **externe** Tristates (an den I/O-Pads) sind immer noch die Art, wie DDR-Speicher und PCIe-Endpunkte sich Pins teilen.

## Extra: Achte auf `'X'`

Wenn zwei Tristate-Puffer gleichzeitig denselben Bus mit unterschiedlichen Werten treiben, würde eine echte Schaltung Rauch erzeugen. Der Simulator zeigt das als `'X'` (unbekannt) in der Waveform. Versuche, ein kleines Testbench mit zwei deiner Puffer an demselben `y` zu bauen, und beobachte, was passiert.
