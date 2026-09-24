## LFSR — corrige el polinomio

Este LFSR de 8 bits debe generar una secuencia pseudoaleatoria de
longitud máxima (periodo 255). El código actual usa un polinomio
no primitivo: solo dos taps se XOR-ean, así que el registro cae en
un ciclo corto y la salida dista mucho de ser aleatoria.

### Especificación

* Reset síncrono activo-alto; valor de reset `00000001`.
* LFSR de Fibonacci con desplazamiento a la **izquierda** y nuevo
  bit en la posición 0: `reg <= reg(6 downto 0) & fb`.
* Realimentación `fb` = XOR de los taps **8, 6, 5, 4** (notación
  1-indexada), es decir `reg(7) xor reg(5) xor reg(4) xor reg(3)`.
  Polinomio: `x^8 + x^6 + x^5 + x^4 + 1`.

### Pista

Los polinomios de dos taps sobre GF(2) casi nunca son primitivos
a longitud 8. El conjunto canónico para el LFSR máximo de 8 bits
es `{8, 6, 5, 4}`.
