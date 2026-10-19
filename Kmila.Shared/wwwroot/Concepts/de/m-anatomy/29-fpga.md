# FPGA — der ganze Chip

Ein **FPGA** ist ein Meer dieser Teile auf einem Chip: ein Raster aus **CLBs** (Logikzellen), Spalten aus **Block-RAM** und **DSP**, ein **I/O-Ring**, ein **Taktbaum** und das Routing, das alles verbindet.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/29-fpga.html" title="Interaktive Abbildung" loading="lazy"></iframe></div>

Die leuchtenden Pfade sind zwei konfigurierte Signale, die durchs Gewebe geroutet werden. Alles aus diesem Kurs lebt hier: Gatter und Addierer werden LUTs, Register sind die Flip-Flops, und der Router verdrahtet sie durch die Kanäle.

Das ist der ganze Pfad auf einem Chip: der **Takt** taktet ihn, **Gatter** und **Addierer** werden **LUTs** in **Logikzellen**, **Flip-Flops** halten den Zustand, **Speicher** und **DSP** sitzen in Spalten, und das **Interconnect** verdrahtet alles — umprogrammierbar, weil alles Look-up-Tabellen und Schalter sind, geladen aus einem Bitstream.
