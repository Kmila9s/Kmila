# Interconnect & E/A

Das **Interconnect** ist die programmierbare Verdrahtung zwischen Zellen: **Routing-Kanäle**, **Switch-Boxen** an den Kreuzungen, die sie verbinden, und **I/O-Blöcke (IOB)** am Rand zu den Pins.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/28-interconnect.html" title="Interaktive Abbildung" loading="lazy"></iframe></div>

Ein Signal wird von einem I/O-Block entlang der Kanäle und durch die Switch-Boxen zu einem CLB geroutet — der leuchtende Pfad. Das Programmieren der Switch-Boxen verbindet dein Design.

Der Großteil von Fläche und Verzögerung eines FPGA ist eigentlich **Routing**, nicht Logik. Place-and-Route entscheidet, welche Zellen genutzt und wie sie verdrahtet werden — der langsame, clevere Schritt nach der Synthese.
