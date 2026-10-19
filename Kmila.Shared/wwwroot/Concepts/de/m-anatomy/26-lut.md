# Look-up-Tabelle (LUT)

Eine **Look-up-Tabelle (LUT)** ist, wie ein FPGA zu jedem Gatter wird: ein winziger Speicher mit einer Wahrheitstabelle. Die Eingänge bilden eine **Adresse**; der Ausgang ist das dort gespeicherte Bit.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/26-lut.html" title="Interaktive Abbildung" loading="lazy"></iframe></div>

Die Eingänge adressieren die 4 Konfigurationszellen (die geladene Wahrheitstabelle); der Ausgang ist das gewählte Bit. Sieh zu, wie sich die Konfiguration **umprogrammiert** — dieselbe LUT wird UND, dann ODER, dann XOR. Das ist rekonfigurierbare Logik.

In VHDL:

```vhdl
y <= a and b;   -- the synthesizer loads this
                -- truth table into ONE LUT
```

Dein ganzer Entwurf wird auf LUTs abgebildet: der Synthesizer macht aus jedem booleschen Ausdruck Wahrheitstabellen und lädt sie beim Einschalten aus dem Konfigurations-Bitstream. Eine N-Eingangs-LUT kann **jede** Funktion von N Eingängen sein.
