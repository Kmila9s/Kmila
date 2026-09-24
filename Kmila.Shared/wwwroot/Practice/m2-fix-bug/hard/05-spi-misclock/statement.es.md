## SPI — corrige el flanco de muestreo

Este es un generador SPI maestro mínimo. Divide `clk` entre 8 para
producir `sclk` (conmuta en los conteos 3 y 7) y pulsa `sample`
durante un ciclo cuando hay que capturar `miso`. El bug: `sample`
se activa actualmente en el flanco **ascendente** de `sclk`, pero
en SPI modo 0 el esclavo conduce `miso` en el ascendente y el
maestro debe muestrear en el **descendente**.

### Especificación

* `sclk` cambia: sube en `divider = 3`, baja en `divider = 7`.
* `sample` es un pulso de un ciclo igual a `miso` **en el flanco
  descendente de `sclk`**.
* Reset síncrono activo-alto pone todo a 0.

### Pista

Mueve la asignación `sample_int <= miso;` de la rama `divider = 3`
a la rama `divider = 7`.
