## Conversión binario → Gray — rellena la conversión

Un contador binario de 3 bits `b(2..0)` ya está en marcha y se limpia
con reset. El bus de salida `g` se alimenta del vector Gray `gi`. Tu
tarea es calcular `gi` a partir de `b`.

### Especificación

La conversión estándar binario → Gray es:

```
gi(2) = b(2)
gi(1) = b(2) xor b(1)
gi(0) = b(1) xor b(0)
```

Produce la secuencia `000, 001, 011, 010, 110, 111, 101, 100, …`
en la que cada paso cambia un único bit.

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. El contador, el
reset y `g <= gi` están bloqueados.

### Pista

Tres asignaciones concurrentes a los bits de `gi`, o una combinada
con `b(2) & (b(2) xor b(1)) & (b(1) xor b(0))`.
