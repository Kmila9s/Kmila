## Igualdad 4 bits con XOR — encuentra y corrige el error

Este circuito debe entregar `eq = 1` cuando `a = b` y `0` en
otro caso. El autor intentó calcularlo con una cadena larga de
`xor` sobre bits individuales — pero eso da la **paridad**, no
la igualdad.

Encuentra el error y usa la comparación correcta.

### Especificación

* `eq = 1` cuando `a = b`, en otro caso `0`.

### Pista

La igualdad es cierta cuando **cada par** de bits correspondiente
coincide — no cuando hay un número par o impar de unos.
