# Logikzelle (Slice)

Eine **Logikzelle** (oder *Slice*) ist der sich wiederholende Baustein des FPGA: eine **LUT** + ein **Flip-Flop** + ein Ausgangs-**MUX** (plus Übertragskette). Sie liefert einen kombinatorischen oder registrierten Ausgang.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/27-slice.html" title="Interaktive Abbildung" loading="lazy"></iframe></div>

Die Eingänge speisen die LUT; ihr Ergebnis geht direkt hinaus (**kombinatorisch**) oder durch das Flip-Flop (**registriert**); der MUX wählt. Der Takt treibt das Flip-Flop; eine Übertragskette quert die Kachel.

Pack `y <= a and b` und ein `rising_edge(clk)`-Register zusammen, und sie passen in einen Slice — die LUT für die Logik, das Flip-Flop für den Zustand. Moderne Slices fassen mehrere LUTs und Flip-Flops.
