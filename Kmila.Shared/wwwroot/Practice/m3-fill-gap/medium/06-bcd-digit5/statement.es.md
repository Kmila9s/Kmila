## BCD a 7 segmentos — completa el dígito 5

Un decodificador BCD a 7 segmentos está **casi** completo: los dígitos
0-4 y 6-9 ya están implementados en un `case`. Solo falta la entrada
para el dígito 5.

Debes escribir la línea que mapea `digit = "0101"` a su patrón de 7
segmentos. El orden es `seg(6..0) = a..g`, **activo alto** (segmento
encendido = `'1'`).

Para el dígito 5 los segmentos encendidos son `a, c, d, f, g` (los `b`
y `e` apagados), dando:

```
seg = "1011011"
```

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. Las otras ramas del
`case` están bloqueadas.

### Pista

Solo una línea: `when "0101" => seg <= "1011011";`.
