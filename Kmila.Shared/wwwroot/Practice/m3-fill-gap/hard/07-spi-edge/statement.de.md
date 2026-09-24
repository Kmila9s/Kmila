## SPI-Fallflanken-Detektor — Lücke füllen

Ein typisches SPI-Front-End muss die **Fallflanke** des langsamen
`sclk` aus der schnellen `clk`-Domäne erkennen. Dafür halten
wir ein Geschichtsregister `sclk_prev` und vergleichen es mit
dem aktuellen `sclk`.

Das Register, der asynchrone Reset und der
`process(clk, reset)` sind bereits verdrahtet. Du schreibst nur
den **Flanken-Ausdruck**, der den Strobe `sample` erzeugt.

### Spezifikation

* Fallflanke: `sclk_prev = '1'` UND `sclk = '0'`.
* Eine konkurrente Zuweisung:
  ```vhdl
  sample <= sclk_prev and (not sclk);
  ```
* Der Strobe dauert genau einen `clk`-Takt.

### Wo schreiben

Nur die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT` wird
bewertet.
