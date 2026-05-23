# Korrigieren: Der Tristate-Buffer, der immer treibt

Ein Tristate-Buffer ist ein umschaltbarer Ausgang. Wenn das Output-Enable-Signal (`oe`) high ist, gibt er seine Daten an den Bus weiter. Bei low **gibt er den Bus frei** — er treibt hochohmig (`'Z'`), damit andere Geräte reden können.

## Was falsch ist

Öffne das Beispiel und sieh dir den Body an:

```vhdl
y <= d when oe = '0' else 'Z';
```

Lies es laut vor: „`y` ist `d`, **wenn `oe` 0 ist**, sonst `'Z'`." Das ist falsch herum. Du willst, dass der Buffer Daten weitergibt, wenn das Enable **high** ist, und freigibt, wenn es **low** ist.

## Dein Auftrag

- Öffne das Beispiel im Editor.
- Korrigiere die Bedingung in Zeile 12 (die `y <=`-Zuweisung).
- Lass die Simulation laufen und prüfe im Wellenform-Tab:
  - Bei `oe = '0'` soll `y` `'Z'` sein.
  - Bei `oe = '1'` soll `y` `d` folgen.

## Warum Tristate wichtig ist

Tristate-Buffer sind die Art, wie mehrere Treiber sich einen Draht (einen Bus) teilen, ohne sich kurzzuschließen. FPGAs haben in ihrer internen Struktur keine Tristates — sie nutzen stattdessen Multiplexer — aber **externe** Tristates (an den I/O-Pads) sind weiterhin so, wie DDR-Speicher und PCIe-Endpunkte Pins teilen.

## Extra: pass auf `'X'` auf

Wenn zwei Tristate-Buffer denselben Bus gleichzeitig mit unterschiedlichen Werten treiben, würde eine echte Schaltung qualmen. Der Simulator zeigt das als `'X'` (unbekannt) auf der Wellenform. Versuch eine winzige Testbench mit zwei deiner Buffer auf demselben `y` und beobachte, was passiert.
