## UART — corrige el muestreo del bit de start

Esta FSM mínima de recepción UART tiene cuatro estados: `IDLE`,
`START`, `DATA`, `STOP` (codificados como `00`, `01`, `10`, `11`
en `state`). Con el código actual, la FSM sale de `START` a mitad
del periodo de bit y el resto de la trama se muestrea en el lugar
equivocado.

### Especificación

* `clk` de 50 MHz, `BIT_PERIOD = 10` ciclos por bit.
* En `IDLE`: un flanco bajante en `rx` (`rx = '0'`) lleva la FSM a
  `START` inmediatamente, **al inicio del periodo de bit**.
* `START` y `DATA` duran exactamente `BIT_PERIOD` ciclos.
* `STOP` dura un ciclo y vuelve a `IDLE`.

### Pista

Si la constante del periodo de bit vale la mitad de lo correcto,
la FSM muestrea el centro de cada bit en lugar del inicio.
Recalcula la constante a partir de la frecuencia y el baud rate.
