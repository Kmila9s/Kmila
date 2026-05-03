## Multiplicador 4×4 — corrige la extensión de signo asimétrica

Dos valores sin signo de 4 bits `a` y `b` se multiplican para dar
un `product` sin signo de 8 bits. El código actual interpreta por
error `a` como **con signo** mientras mantiene `b` sin signo, lo
que da resultados incorrectos cuando `a(3) = '1'` (es decir,
`a >= 8`).

### Especificación

* `a` y `b` son enteros **sin signo** de 4 bits en `0..15`.
* `product = a * b` interpretado como **sin signo** de 8 bits
  (rango `0..225`).

### Pista

Convierte ambos operandos del mismo modo:
`unsigned(a) * unsigned(b)`. El resultado ya cabe exactamente en
8 bits — no hace falta `resize`.
