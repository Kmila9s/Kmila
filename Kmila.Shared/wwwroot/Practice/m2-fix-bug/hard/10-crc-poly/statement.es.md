## CRC-8 — corrige el polinomio

Este generador serie de CRC-8 XOR-ea un bit de realimentación en
posiciones concretas del registro de desplazamiento, elegidas por
el polinomio. El código actual usa el polinomio equivocado:
`0x07 = x^2 + x + 1` en lugar del previsto `0x1D = x^4 + x^3 + x^2 + 1`.
Los CRC calculados con la versión con bug no coincidirán con el
checksum esperado.

### Especificación

* Reset síncrono activo-alto; `crc` se pone a `00000000`.
* En cada ciclo: `fb = reg(7) xor din`.
* `reg(0) <= fb`, y para el polinomio `0x1D`:
  * `reg(2) <= reg(1) xor fb`
  * `reg(3) <= reg(2) xor fb`
  * `reg(4) <= reg(3) xor fb`
  * el resto de bits desplazan sin cambios.

### Pista

Los taps XOR siguen los coeficientes del polinomio `0x1D` (binario
`00011101`). La versión con bug pone XOR solo en las posiciones
1 y 2, lo que corresponde a `0x07`.
