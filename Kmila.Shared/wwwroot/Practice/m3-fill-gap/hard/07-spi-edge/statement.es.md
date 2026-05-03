## Detector de flanco de bajada SPI — completa el hueco

Un front-end SPI típico necesita detectar el **flanco de bajada**
de la señal lenta `sclk` desde el dominio rápido `clk`. Lo
hacemos guardando un registro de historia `sclk_prev` y
comparándolo con el `sclk` actual.

El registro de historia, el reset asíncrono y el
`process(clk, reset)` ya están cableados. Tú sólo escribes la
**expresión de flanco** que produce el strobe `sample`.

### Especificación

* Condición de flanco de bajada: `sclk_prev = '1'` Y `sclk = '0'`.
* Una sola asignación concurrente:
  ```vhdl
  sample <= sclk_prev and (not sclk);
  ```
* El strobe dura un ciclo de `clk`.

### Dónde escribir

Sólo la línea entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúa.
No modifiques el proceso con registro — está bloqueado.
