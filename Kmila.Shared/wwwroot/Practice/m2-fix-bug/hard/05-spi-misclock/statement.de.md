## SPI — repariere die Sample-Flanke

Dies ist ein minimaler SPI-Master-Taktgenerator. Er teilt `clk`
durch 8, um `sclk` zu erzeugen (Wechsel bei Divider 3 und 7), und
erzeugt einen Ein-Takt-Impuls auf `sample`, wenn `miso` übernommen
werden soll. Bug: `sample` wird derzeit an der **steigenden** Flanke
von `sclk` aktiviert, aber im SPI-Modus 0 treibt der Slave `miso`
auf der steigenden, und der Master muss an der **fallenden**
Flanke abtasten.

### Spezifikation

* `sclk` wechselt: steigt bei `divider = 3`, fällt bei `divider = 7`.
* `sample` ist ein Ein-Takt-Impuls gleich `miso` **an der fallenden
  Flanke von `sclk`**.
* Synchroner, high-aktiver Reset setzt alles auf 0.

### Hinweis

Verschiebe die Zuweisung `sample_int <= miso;` aus dem Zweig
`divider = 3` in den Zweig `divider = 7`.
