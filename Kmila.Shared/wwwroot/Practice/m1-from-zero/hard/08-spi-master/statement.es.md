## Maestro SPI (shift)

Implementa un maestro SPI mínimo que desplaza un byte MSB-first cuando `start='1'`.

### Especificación

- Un contador `phase` de 4 bits marca el ritmo; activo, incrementa en cada flanco.
- `sclk = phase(0)` mientras esté activo — produce 8 pares alto/bajo.
- `mosi = shreg(7)` mientras esté activo — es decir, el MSB actual.
- Desplaza `shreg` a la izquierda cuando `phase(0) = '1'`.
- Reset limpia todo. La máquina vuelve a inactiva tras 16 fases (las pruebas solo miran la ventana inicial).

### Pista

Un flag `active` mantiene las salidas en `'0'` mientras esté inactiva.
