## Desplazamiento ALU a la izquierda — completa el hueco

Un barrel shifter está **casi** terminado. La entidad ya declara
la entrada de 8 bits `a`, la cantidad de desplazamiento de 3 bits
`sh`, y la salida `y`. El evaluador ya ha colocado el
`process(a, sh)` que envuelve tu código.

Tu tarea es asignar a `y` el valor de `a` desplazado a la
**izquierda** por `sh` posiciones, rellenando con ceros por la
derecha.

### Especificación

* `numeric_std` ya está importado.
* Convierte `a` a `unsigned`, la cantidad con
  `to_integer(unsigned(sh))`, y usa `shift_left(...)`.
* Convierte el resultado con `std_logic_vector(...)`.
* Los bits que pasan del MSB se descartan; los LSB quedan a `0`.

### Pista

Una sola línea basta:

```vhdl
y <= std_logic_vector(shift_left(unsigned(a), to_integer(unsigned(sh))));
```

### Dónde escribir

Sólo las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúan.
