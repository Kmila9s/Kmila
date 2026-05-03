## UART — corrigez l'échantillonnage du bit de start

Cette FSM minimale de réception UART possède quatre états : `IDLE`,
`START`, `DATA`, `STOP` (codés `00`, `01`, `10`, `11` sur `state`).
Avec le code actuel, la FSM quitte `START` à la moitié du temps-bit
et la suite de la trame est échantillonnée au mauvais endroit.

### Spécification

* `clk` à 50 MHz, `BIT_PERIOD = 10` cycles par bit.
* Depuis `IDLE` : un front descendant sur `rx` (`rx = '0'`) fait
  passer la FSM à `START` immédiatement, **au début du temps-bit**.
* `START` et `DATA` durent exactement `BIT_PERIOD` cycles.
* `STOP` dure un cycle puis retourne à `IDLE`.

### Indice

Si la constante de temps-bit vaut la moitié de la vraie valeur,
la FSM échantillonne le milieu de chaque bit au lieu du début.
Recalculez la constante à partir de l'horloge et du baud rate.
